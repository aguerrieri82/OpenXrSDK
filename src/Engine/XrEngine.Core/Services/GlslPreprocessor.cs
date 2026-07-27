using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;

namespace XrEngine;

public readonly struct GlslRuntimeDefine
{
    public GlslRuntimeDefine(string symbol, string? uniformName = null)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Runtime define symbol is required.", nameof(symbol));

        Symbol = symbol;
        UniformName = string.IsNullOrWhiteSpace(uniformName) ? ToUniformName(symbol) : uniformName;
    }

    public string Symbol { get; }

    public string UniformName { get; }

    private static string ToUniformName(string symbol)
    {
        var result = new StringBuilder(symbol.Length + 1);
        result.Append('u');
        bool upper = true;

        foreach (char c in symbol)
        {
            if (c == '_')
            {
                upper = true;
                continue;
            }

            result.Append(upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
            upper = false;
        }

        return result.ToString();
    }
}

public sealed class GlslPreprocessor
{
    private readonly Func<string, string> _includeResolver;

    public GlslPreprocessor(Func<string, string> includeResolver)
    {
        _includeResolver = includeResolver ?? throw new ArgumentNullException(nameof(includeResolver));
    }

    public bool EmitConditionComments { get; set; }

    public int MaxIncludeDepth { get; set; } = 64;

    public string Process(string sourceName, IReadOnlyList<string>? defines = null, IReadOnlyList<GlslRuntimeDefine>? runtimeDefines = null)
    {
        if (sourceName == null)
            throw new ArgumentNullException(nameof(sourceName));

        sourceName = NormalizePath(sourceName);
        var context = new Context(this, sourceName, runtimeDefines);

        if (defines != null)
        {
            foreach (var define in defines)
            {
                if (string.IsNullOrWhiteSpace(define))
                    continue;

                context.Define(define, "<external>", 0);
            }
        }

        return context.Complete(context.ProcessFile(sourceName, 0));
    }

    private static string NormalizePath(string path)
    {
        return Path.GetRelativePath(".", path).Replace('\\', '/');
    }

    private sealed class Context
    {
        private readonly GlslPreprocessor _owner;
        private readonly Dictionary<string, Macro> _macros = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _fileIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> _included = new(StringComparer.Ordinal);
        private readonly Dictionary<string, GlslRuntimeDefine>? _runtimeDefines;
        private readonly HashSet<string>? _usedRuntimeDefines;
        private int _nextFileId = 1;
        private int _version = 100;

        public Context(GlslPreprocessor owner, string sourceName, IReadOnlyList<GlslRuntimeDefine>? runtimeDefines)
        {
            _owner = owner;
            _fileIds[sourceName] = 0;

            if (runtimeDefines is { Count: > 0 })
            {
                _runtimeDefines = new Dictionary<string, GlslRuntimeDefine>(runtimeDefines.Count, StringComparer.Ordinal);
                _usedRuntimeDefines = new HashSet<string>(StringComparer.Ordinal);

                foreach (var runtimeDefine in runtimeDefines)
                {
                    if (!_runtimeDefines.TryAdd(runtimeDefine.Symbol, runtimeDefine))
                        throw new ArgumentException($"Duplicate runtime define '{runtimeDefine.Symbol}'.", nameof(runtimeDefines));
                }
            }
        }

        public string Complete(string source)
        {
            if (_usedRuntimeDefines == null || _usedRuntimeDefines.Count == 0)
                return source;

            var declarations = new StringBuilder();
            foreach (string symbol in _usedRuntimeDefines)
                declarations.Append("uniform bool ").Append(_runtimeDefines![symbol].UniformName).Append(';').Append('\n');

            int insert = 0;
            foreach (var line in ReadLogicalLines(source))
            {
                string trimmed = line.Text.TrimStart();
                if (trimmed.StartsWith("#version", StringComparison.Ordinal) ||
                    trimmed.StartsWith("#extension", StringComparison.Ordinal) ||
                    trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal))
                {
                    int next = source.IndexOf('\n', insert);
                    if (next < 0)
                        return source + "\n" + declarations;
                    insert = next + 1;
                    continue;
                }

                break;
            }

            return source.Insert(insert, declarations.ToString());
        }

        public string ProcessFile(string fileName, int includeDepth)
        {
            fileName = NormalizePath(fileName);

            if (!_included.Add(fileName))
                return string.Empty;

            string source;

            try
            {
                source = _owner._includeResolver(fileName);
            }
            catch (Exception ex) when (ex is not GlslPreprocessorException)
            {
                throw new GlslPreprocessorException(fileName, 0, $"Unable to resolve source '{fileName}'.", ex);
            }

            if (string.IsNullOrEmpty(source))
                throw new GlslPreprocessorException(fileName, 0, $"Source '{fileName}' not found.");

            return ProcessSource(source, fileName, GetFileId(fileName), includeDepth);
        }

        private string ProcessSource(string source, string fileName, int fileId, int includeDepth)
        {
            if (includeDepth > _owner.MaxIncludeDepth)
                throw new GlslPreprocessorException(fileName, 0, $"Maximum include depth of {_owner.MaxIncludeDepth} exceeded.");

            var result = new StringBuilder(source.Length);
            var conditions = new Stack<ConditionalFrame>();
            bool inBlockComment = false;
            var scope = new GlslScopeTracker();

            foreach (var logicalLine in ReadLogicalLines(source))
            {
                bool blockCommentAtStart = inBlockComment;
                string directiveView = ReplaceCommentsWithSpaces(logicalLine.Text, ref inBlockComment);
                bool active = conditions.Count == 0 || conditions.Peek().CurrentActive;

                if (TryReadDirective(directiveView, out var directive, out var argument))
                {
                    switch (directive)
                    {
                        case "":
                            break;

                        case "define":
                            if (active)
                                Define(argument, fileName, logicalLine.LineNumber);
                            break;

                        case "undef":
                            if (active)
                                Undef(argument, fileName, logicalLine.LineNumber);
                            break;

                        case "include":
                            if (active)
                            {
                                string includeName = ParseInclude(argument, fileId, logicalLine.LineNumber, fileName);
                                string includePath = NormalizePath(Path.Join(Path.GetDirectoryName(fileName) ?? "", includeName));

                                result.Append(ProcessFile(includePath, includeDepth + 1));
                            }
                            break;

                        case "if":
                            {
                                bool parentActive = active;
                                if (TryBuildRuntimeCondition(argument, fileName, logicalLine.LineNumber, out string? runtimeCondition))
                                {
                                    bool emitRuntime = scope.IsInsideFunction;
                                    var frame = new ConditionalFrame(parentActive, parentActive, false,
                                        $"#if {argument.Trim()}", false, false, true, emitRuntime);
                                    conditions.Push(frame);
                                    if (parentActive && emitRuntime)
                                        result.Append("if (").Append(runtimeCondition).Append(")\n{\n");
                                }
                                else
                                {
                                    bool branchActive = parentActive && EvaluateCondition(argument, fileId, logicalLine.LineNumber, fileName);
                                    var frame = new ConditionalFrame(parentActive, branchActive, branchActive,
                                        $"#if {argument.Trim()}", parentActive);
                                    conditions.Push(frame);
                                    EmitBranchStart(result, frame);
                                }
                                break;
                            }

                        case "ifdef":
                            {
                                string name = ParseSingleIdentifier(argument, fileName, logicalLine.LineNumber, "#ifdef");
                                bool parentActive = active;
                                if (TryGetRuntimeUniform(name, fileName, logicalLine.LineNumber, out string? uniformName))
                                {
                                    bool emitRuntime = scope.IsInsideFunction;
                                    var frame = new ConditionalFrame(parentActive, parentActive, false,
                                        $"#ifdef {name}", false, false, true, emitRuntime);
                                    conditions.Push(frame);
                                    if (parentActive && emitRuntime)
                                        result.Append("if (").Append(uniformName).Append(")\n{\n");
                                }
                                else
                                {
                                    bool branchActive = parentActive && IsDefined(name);
                                    var frame = new ConditionalFrame(parentActive, branchActive, branchActive,
                                        $"#ifdef {name}", parentActive);
                                    conditions.Push(frame);
                                    EmitBranchStart(result, frame);
                                }
                                break;
                            }

                        case "ifndef":
                            {
                                string name = ParseSingleIdentifier(argument, fileName, logicalLine.LineNumber, "#ifndef");
                                bool parentActive = active;
                                if (TryGetRuntimeUniform(name, fileName, logicalLine.LineNumber, out string? uniformName))
                                {
                                    bool emitRuntime = scope.IsInsideFunction;
                                    var frame = new ConditionalFrame(parentActive, parentActive, false,
                                        $"#ifndef {name}", false, false, true, emitRuntime);
                                    conditions.Push(frame);
                                    if (parentActive && emitRuntime)
                                        result.Append("if (!").Append(uniformName).Append(")\n{\n");
                                }
                                else
                                {
                                    bool branchActive = parentActive && !IsDefined(name);
                                    var frame = new ConditionalFrame(parentActive, branchActive, branchActive,
                                        $"#ifndef {name}", parentActive);
                                    conditions.Push(frame);
                                    EmitBranchStart(result, frame);
                                }
                                break;
                            }

                        case "elif":
                            {
                                if (conditions.Count == 0)
                                    throw new GlslPreprocessorException(fileName, logicalLine.LineNumber, "#elif without matching #if.");

                                var frame = conditions.Pop();
                                if (frame.SeenElse)
                                    throw new GlslPreprocessorException(fileName, logicalLine.LineNumber, "#elif after #else.");

                                if (frame.IsRuntime)
                                {
                                    if (!TryBuildRuntimeCondition(argument, fileName, logicalLine.LineNumber, out string? runtimeCondition))
                                        throw new GlslPreprocessorException(fileName, logicalLine.LineNumber,
                                            "A runtime conditional chain cannot contain a compile-time #elif.");

                                    frame = frame with { BranchText = $"#elif {argument.Trim()}" };
                                    conditions.Push(frame);
                                    if (frame.ParentActive && frame.EmitRuntimeControlFlow)
                                        result.Append("}\nelse if (").Append(runtimeCondition).Append(")\n{\n");
                                    break;
                                }

                                if (ContainsRuntimeSymbol(argument))
                                    throw new GlslPreprocessorException(fileName, logicalLine.LineNumber,
                                        "A compile-time conditional chain cannot switch to a runtime #elif.");

                                EmitBranchEnd(result, frame);

                                bool branchActive = frame.ParentActive && !frame.AnyTaken &&
                                    EvaluateCondition(argument, fileId, logicalLine.LineNumber, fileName);

                                frame = frame with
                                {
                                    CurrentActive = branchActive,
                                    AnyTaken = frame.AnyTaken || branchActive,
                                    BranchText = $"#elif {argument.Trim()}"
                                };

                                conditions.Push(frame);
                                EmitBranchStart(result, frame);
                                break;
                            }

                        case "else":
                            {
                                if (conditions.Count == 0)
                                    throw new GlslPreprocessorException(fileName, logicalLine.LineNumber, "#else without matching #if.");

                                var frame = conditions.Pop();
                                if (frame.SeenElse)
                                    throw new GlslPreprocessorException(fileName, logicalLine.LineNumber, "Duplicate #else.");

                                if (frame.IsRuntime)
                                {
                                    frame = frame with { SeenElse = true, BranchText = "#else" };
                                    conditions.Push(frame);
                                    if (frame.ParentActive && frame.EmitRuntimeControlFlow)
                                        result.Append("}\nelse\n{\n");
                                    break;
                                }

                                EmitBranchEnd(result, frame);

                                bool branchActive = frame.ParentActive && !frame.AnyTaken;
                                frame = frame with
                                {
                                    CurrentActive = branchActive,
                                    AnyTaken = true,
                                    SeenElse = true,
                                    BranchText = "#else"
                                };

                                conditions.Push(frame);
                                EmitBranchStart(result, frame);
                                break;
                            }

                        case "endif":
                            {
                                if (conditions.Count == 0)
                                    throw new GlslPreprocessorException(fileName, logicalLine.LineNumber, "#endif without matching #if.");

                                var frame = conditions.Pop();
                                if (frame.IsRuntime)
                                {
                                    if (frame.ParentActive && frame.EmitRuntimeControlFlow)
                                        result.Append("}\n");
                                }
                                else
                                {
                                    EmitBranchEnd(result, frame);
                                }
                                break;
                            }

                        case "error":
                            if (active)
                            {
                                string message = Expand(argument, fileId, logicalLine.LineNumber, fileName).Trim();
                                throw new GlslPreprocessorException(fileName, logicalLine.LineNumber,
                                    message.Length == 0 ? "#error" : message);
                            }
                            break;

                        case "version":
                            if (active)
                            {
                                UpdateVersion(argument, fileName, logicalLine.LineNumber);
                                result.Append(logicalLine.Text).Append('\n');
                            }
                            break;

                        case "extension":
                        case "pragma":
                            if (active)
                                result.Append(logicalLine.Text).Append('\n');
                            break;

                        case "line":
                            if (active)
                            {
                                string expanded = Expand(argument, fileId, logicalLine.LineNumber, fileName).Trim();
                                result.Append("#line ").Append(expanded).Append('\n');
                            }
                            break;

                        default:
                            throw new GlslPreprocessorException(fileName, logicalLine.LineNumber,
                                $"Unsupported preprocessor directive '#{directive}'.");
                    }

                    continue;
                }

                if (!active)
                    continue;

                result.Append(ExpandLinePreservingComments(logicalLine.Text, blockCommentAtStart,
                    fileId, logicalLine.LineNumber, fileName)).Append('\n');
                scope.Process(directiveView);
            }

            if (conditions.Count != 0)
                throw new GlslPreprocessorException(fileName, 0, "Unterminated conditional block; missing #endif.");

            return result.ToString();
        }

        public void Define(string definition, string fileName, int lineNumber)
        {
            int p = 0;
            SkipHorizontalWhitespace(definition, ref p);

            string name = ReadIdentifier(definition, ref p);
            if (name.Length == 0)
                throw new GlslPreprocessorException(fileName, lineNumber, "Invalid #define: macro name expected.");

            if (name.StartsWith("GL_", StringComparison.Ordinal))
                throw new GlslPreprocessorException(fileName, lineNumber, $"Macro name '{name}' is reserved by GLSL.");

            List<string>? parameters = null;

            // For a function-like macro, '(' must immediately follow the macro name.
            if (p < definition.Length && definition[p] == '(')
            {
                p++;
                parameters = new List<string>();
                SkipHorizontalWhitespace(definition, ref p);

                if (p < definition.Length && definition[p] == ')')
                {
                    p++;
                }
                else
                {
                    while (true)
                    {
                        SkipHorizontalWhitespace(definition, ref p);
                        string parameter = ReadIdentifier(definition, ref p);
                        if (parameter.Length == 0)
                            throw new GlslPreprocessorException(fileName, lineNumber,
                                $"Invalid parameter list for macro '{name}'.");

                        if (parameters.Contains(parameter, StringComparer.Ordinal))
                            throw new GlslPreprocessorException(fileName, lineNumber,
                                $"Duplicate macro parameter '{parameter}' in '{name}'.");

                        parameters.Add(parameter);
                        SkipHorizontalWhitespace(definition, ref p);

                        if (p >= definition.Length)
                            throw new GlslPreprocessorException(fileName, lineNumber,
                                $"Unterminated parameter list for macro '{name}'.");

                        if (definition[p] == ')')
                        {
                            p++;
                            break;
                        }

                        if (definition[p] != ',')
                            throw new GlslPreprocessorException(fileName, lineNumber,
                                $"Expected ',' or ')' in parameter list for macro '{name}'.");

                        p++;
                    }
                }
            }

            SkipHorizontalWhitespace(definition, ref p);
            string body = p < definition.Length ? definition[p..] : string.Empty;
            var macro = new Macro(name, parameters, body);

            if (_macros.TryGetValue(name, out var existing) && !MacroEquals(existing, macro))
                throw new GlslPreprocessorException(fileName, lineNumber, $"Macro '{name}' redefined with a different value.");

            _macros[name] = macro;
        }

        private static bool MacroEquals(Macro a, Macro b)
        {
            if (a.Parameters == null || b.Parameters == null)
            {
                if (a.Parameters != null || b.Parameters != null)
                    return false;
            }
            else if (!a.Parameters.SequenceEqual(b.Parameters, StringComparer.Ordinal))
            {
                return false;
            }

            var aBody = Tokenize(a.Body).Where(t => t.Kind != TokenKind.WhiteSpace).Select(t => t.Text);
            var bBody = Tokenize(b.Body).Where(t => t.Kind != TokenKind.WhiteSpace).Select(t => t.Text);
            return aBody.SequenceEqual(bBody, StringComparer.Ordinal);
        }

        private void Undef(string argument, string fileName, int lineNumber)
        {
            string name = ParseSingleIdentifier(argument, fileName, lineNumber, "#undef");
            _macros.Remove(name);
        }

        private bool IsDefined(string name)
        {
            return name is "__LINE__" or "__FILE__" or "__VERSION__" || _macros.ContainsKey(name);
        }

        private int GetFileId(string fileName)
        {
            if (_fileIds.TryGetValue(fileName, out int id))
                return id;

            id = _nextFileId++;
            _fileIds[fileName] = id;
            return id;
        }

        private string ParseInclude(string argument, int fileId, int lineNumber, string fileName)
        {
            string value = Expand(argument, fileId, lineNumber, fileName).Trim();

            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                return value[1..^1];

            if (value.Length >= 2 && value[0] == '<' && value[^1] == '>')
                return value[1..^1].Trim();

            // #include is an engine extension rather than a core GLSL directive, so bare names are useful too.
            if (value.Length > 0)
                return value;

            throw new GlslPreprocessorException(fileName, lineNumber, "Empty #include target.");
        }

        private bool ContainsRuntimeSymbol(string expression)
        {
            if (_runtimeDefines == null)
                return false;

            foreach (var token in Tokenize(expression))
            {
                if (token.Kind == TokenKind.Identifier && token.Text != "defined" && _runtimeDefines.ContainsKey(token.Text))
                    return true;
            }

            return false;
        }

        private bool TryGetRuntimeUniform(string symbol, string fileName, int lineNumber, out string? uniformName)
        {
            uniformName = null;
            if (_runtimeDefines == null || !_runtimeDefines.TryGetValue(symbol, out var runtimeDefine))
                return false;

            ValidateRuntimeMacro(symbol, fileName, lineNumber);
            _usedRuntimeDefines!.Add(symbol);
            uniformName = runtimeDefine.UniformName;
            return true;
        }

        private void ValidateRuntimeMacro(string symbol, string fileName, int lineNumber)
        {
            if (!_macros.TryGetValue(symbol, out var macro))
                return;

            if (macro.Parameters != null || !string.IsNullOrWhiteSpace(macro.Body))
                throw new GlslPreprocessorException(fileName, lineNumber,
                    $"Runtime define '{symbol}' must be a valueless object-like macro.");
        }

        private bool TryBuildRuntimeCondition(string expression, string fileName, int lineNumber, out string? runtimeExpression)
        {
            runtimeExpression = null;
            if (_runtimeDefines == null)
                return false;

            var tokens = Tokenize(expression);
            bool hasRuntime = false;
            var referenced = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Kind != TokenKind.Identifier)
                    continue;

                if (token.Text == "defined")
                {
                    int j = NextSignificant(tokens, i + 1);
                    if (j < 0)
                        throw new GlslPreprocessorException(fileName, lineNumber, "Identifier expected after defined.");

                    if (tokens[j].Text == "(")
                    {
                        int nameIndex = NextSignificant(tokens, j + 1);
                        if (nameIndex < 0 || tokens[nameIndex].Kind != TokenKind.Identifier)
                            throw new GlslPreprocessorException(fileName, lineNumber, "Identifier expected inside defined(...).");
                        referenced.Add(tokens[nameIndex].Text);
                    }
                    else
                    {
                        if (tokens[j].Kind != TokenKind.Identifier)
                            throw new GlslPreprocessorException(fileName, lineNumber, "Identifier expected after defined.");
                        referenced.Add(tokens[j].Text);
                    }

                    continue;
                }

                referenced.Add(token.Text);
            }

            foreach (string symbol in referenced)
            {
                if (_runtimeDefines.ContainsKey(symbol))
                {
                    hasRuntime = true;
                    continue;
                }

                if (hasRuntime || referenced.Any(_runtimeDefines.ContainsKey))
                    throw new GlslPreprocessorException(fileName, lineNumber,
                        $"Runtime condition mixes mapped symbol(s) with unmapped symbol '{symbol}'.");
            }

            if (!hasRuntime)
                return false;

            foreach (string symbol in referenced)
                ValidateRuntimeMacro(symbol, fileName, lineNumber);

            var output = new StringBuilder(expression.Length);
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Kind == TokenKind.WhiteSpace)
                {
                    output.Append(token.Text);
                    continue;
                }

                if (token.Kind == TokenKind.Identifier && token.Text == "defined")
                {
                    int j = NextSignificant(tokens, i + 1);
                    string symbol;
                    int end;
                    if (tokens[j].Text == "(")
                    {
                        int nameIndex = NextSignificant(tokens, j + 1);
                        int close = NextSignificant(tokens, nameIndex + 1);
                        if (close < 0 || tokens[close].Text != ")")
                            throw new GlslPreprocessorException(fileName, lineNumber, "Missing ')' after defined(...).");
                        symbol = tokens[nameIndex].Text;
                        end = close;
                    }
                    else
                    {
                        symbol = tokens[j].Text;
                        end = j;
                    }

                    output.Append(_runtimeDefines[symbol].UniformName);
                    _usedRuntimeDefines!.Add(symbol);
                    i = end;
                    continue;
                }

                if (token.Kind == TokenKind.Identifier)
                {
                    output.Append(_runtimeDefines[token.Text].UniformName);
                    _usedRuntimeDefines!.Add(token.Text);
                    continue;
                }

                if (token.Kind == TokenKind.Number)
                {
                    if (token.Text == "0")
                        output.Append("false");
                    else if (token.Text == "1")
                        output.Append("true");
                    else
                        throw new GlslPreprocessorException(fileName, lineNumber,
                            $"Only boolean literals 0 and 1 are supported in runtime conditions, got '{token.Text}'.");
                    continue;
                }

                if (token.Text is not "!" and not "&&" and not "||" and not "(" and not ")")
                    throw new GlslPreprocessorException(fileName, lineNumber,
                        $"Operator '{token.Text}' is not supported in runtime boolean conditions.");

                output.Append(token.Text);
            }

            runtimeExpression = output.ToString();
            return true;
        }

        private bool EvaluateCondition(string expression, int fileId, int lineNumber, string fileName)
        {
            string withDefined = ReplaceDefinedOperators(expression, fileName, lineNumber);
            string expanded = Expand(withDefined, fileId, lineNumber, fileName);
            var parser = new ExpressionParser(expanded, fileName, lineNumber);
            return parser.Parse() != 0;
        }

        private string ReplaceDefinedOperators(string expression, string fileName, int lineNumber)
        {
            var tokens = Tokenize(expression);
            var result = new StringBuilder(expression.Length);

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Kind != TokenKind.Identifier || token.Text != "defined")
                {
                    result.Append(token.Text);
                    continue;
                }

                int j = NextSignificant(tokens, i + 1);
                if (j < 0)
                    throw new GlslPreprocessorException(fileName, lineNumber, "Identifier expected after defined.");

                string name;
                int end;

                if (tokens[j].Text == "(")
                {
                    int nameIndex = NextSignificant(tokens, j + 1);
                    if (nameIndex < 0 || tokens[nameIndex].Kind != TokenKind.Identifier)
                        throw new GlslPreprocessorException(fileName, lineNumber, "Identifier expected inside defined(...). ");

                    int close = NextSignificant(tokens, nameIndex + 1);
                    if (close < 0 || tokens[close].Text != ")")
                        throw new GlslPreprocessorException(fileName, lineNumber, "Missing ')' after defined(...). ");

                    name = tokens[nameIndex].Text;
                    end = close;
                }
                else
                {
                    if (tokens[j].Kind != TokenKind.Identifier)
                        throw new GlslPreprocessorException(fileName, lineNumber, "Identifier expected after defined.");

                    name = tokens[j].Text;
                    end = j;
                }

                result.Append(IsDefined(name) ? '1' : '0');
                i = end;
            }

            return result.ToString();
        }

        private string ExpandLinePreservingComments(string line, bool startsInBlockComment,
            int fileId, int lineNumber, string fileName)
        {
            var result = new StringBuilder(line.Length);
            int start = 0;
            bool inBlock = startsInBlockComment;

            for (int i = 0; i < line.Length; i++)
            {
                if (inBlock)
                {
                    int end = line.IndexOf("*/", i, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        result.Append(line, start, line.Length - start);
                        return result.ToString();
                    }

                    result.Append(line, start, end + 2 - start);
                    i = end + 1;
                    start = i + 1;
                    inBlock = false;
                    continue;
                }

                if (i + 1 >= line.Length || line[i] != '/')
                    continue;

                if (line[i + 1] == '/')
                {
                    result.Append(Expand(line[start..i], fileId, lineNumber, fileName));
                    result.Append(line[i..]);
                    return result.ToString();
                }

                if (line[i + 1] == '*')
                {
                    result.Append(Expand(line[start..i], fileId, lineNumber, fileName));
                    start = i;
                    inBlock = true;
                    i++;
                }
            }

            if (start < line.Length)
            {
                if (inBlock)
                    result.Append(line[start..]);
                else
                    result.Append(Expand(line[start..], fileId, lineNumber, fileName));
            }

            return result.ToString();
        }

        private string Expand(string text, int fileId, int lineNumber, string fileName)
        {
            var tokens = Tokenize(text);
            var expanded = ExpandTokens(tokens, fileId, lineNumber, fileName, new HashSet<string>(StringComparer.Ordinal), 0);
            return Join(expanded);
        }

        private List<Token> ExpandTokens(List<Token> tokens, int fileId, int lineNumber, string fileName,
            HashSet<string> disabled, int depth)
        {
            if (depth > 128)
                throw new GlslPreprocessorException(fileName, lineNumber, "Macro expansion depth exceeded 128.");

            var output = new List<Token>(tokens.Count);

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Kind != TokenKind.Identifier)
                {
                    output.Add(token);
                    continue;
                }

                if (token.Text == "__LINE__")
                {
                    output.Add(new Token(TokenKind.Number, lineNumber.ToString(CultureInfo.InvariantCulture)));
                    continue;
                }

                if (token.Text == "__FILE__")
                {
                    output.Add(new Token(TokenKind.Number, fileId.ToString(CultureInfo.InvariantCulture)));
                    continue;
                }

                if (token.Text == "__VERSION__")
                {
                    output.Add(new Token(TokenKind.Number, _version.ToString(CultureInfo.InvariantCulture)));
                    continue;
                }

                if (disabled.Contains(token.Text) || !_macros.TryGetValue(token.Text, out var macro))
                {
                    output.Add(token);
                    continue;
                }

                var nextDisabled = new HashSet<string>(disabled, StringComparer.Ordinal) { macro.Name };

                if (macro.Parameters == null)
                {
                    var replacement = ExpandTokens(Tokenize(macro.Body), fileId, lineNumber, fileName, nextDisabled, depth + 1);
                    output.AddRange(replacement);
                    continue;
                }

                int open = NextSignificant(tokens, i + 1);
                if (open < 0 || tokens[open].Text != "(")
                {
                    output.Add(token);
                    continue;
                }

                var args = ReadMacroArguments(tokens, open, out int close, fileName, lineNumber);
                if (args.Count != macro.Parameters.Count)
                {
                    // A zero-parameter invocation is tokenized as one empty argument by the generic reader.
                    if (!(macro.Parameters.Count == 0 && args.Count == 1 && IsAllWhitespace(args[0])))
                    {
                        throw new GlslPreprocessorException(fileName, lineNumber,
                            $"Macro '{macro.Name}' expects {macro.Parameters.Count} argument(s), got {args.Count}.");
                    }

                    args.Clear();
                }

                var replacementTokens = SubstituteMacro(macro, args, fileId, lineNumber, fileName, nextDisabled, depth + 1);
                replacementTokens = ExpandTokens(replacementTokens, fileId, lineNumber, fileName, nextDisabled, depth + 1);
                output.AddRange(replacementTokens);
                i = close;
            }

            return output;
        }

        private List<Token> SubstituteMacro(Macro macro, List<List<Token>> args, int fileId, int lineNumber,
            string fileName, HashSet<string> disabled, int depth)
        {
            var rawArgs = new Dictionary<string, List<Token>>(StringComparer.Ordinal);
            var expandedArgs = new Dictionary<string, List<Token>>(StringComparer.Ordinal);

            for (int i = 0; i < macro.Parameters!.Count; i++)
            {
                string name = macro.Parameters[i];
                rawArgs[name] = args[i];
                expandedArgs[name] = ExpandTokens(args[i], fileId, lineNumber, fileName,
                    new HashSet<string>(StringComparer.Ordinal), depth + 1);
            }

            var body = Tokenize(macro.Body);
            var substituted = new List<Token>(body.Count);

            for (int i = 0; i < body.Count; i++)
            {
                var token = body[i];
                if (token.Kind != TokenKind.Identifier || !rawArgs.TryGetValue(token.Text, out var raw))
                {
                    substituted.Add(token);
                    continue;
                }

                int prev = PreviousSignificant(body, i - 1);
                int next = NextSignificant(body, i + 1);
                bool pasted = (prev >= 0 && body[prev].Text == "##") || (next >= 0 && body[next].Text == "##");
                substituted.AddRange(CloneTokens(pasted ? raw : expandedArgs[token.Text]));
            }

            return ApplyTokenPasting(substituted, fileName, lineNumber);
        }

        private static List<Token> ApplyTokenPasting(List<Token> tokens, string fileName, int lineNumber)
        {
            while (true)
            {
                int paste = -1;
                for (int i = 0; i < tokens.Count; i++)
                {
                    if (tokens[i].Text == "##")
                    {
                        paste = i;
                        break;
                    }
                }

                if (paste < 0)
                    return tokens;

                int left = PreviousSignificant(tokens, paste - 1);
                int right = NextSignificant(tokens, paste + 1);
                if (left < 0 || right < 0)
                    throw new GlslPreprocessorException(fileName, lineNumber, "'##' requires a token on both sides.");

                string merged = tokens[left].Text + tokens[right].Text;
                var check = Tokenize(merged).Where(t => t.Kind != TokenKind.WhiteSpace).ToList();
                if (check.Count != 1 || check[0].Text != merged)
                    throw new GlslPreprocessorException(fileName, lineNumber,
                        $"Token pasting produced invalid token '{merged}'.");

                tokens.RemoveRange(left, right - left + 1);
                tokens.Insert(left, check[0]);
            }
        }

        private static List<List<Token>> ReadMacroArguments(List<Token> tokens, int open, out int close,
            string fileName, int lineNumber)
        {
            var args = new List<List<Token>>();
            var current = new List<Token>();
            int depth = 0;

            for (int i = open + 1; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Text == "(")
                {
                    depth++;
                    current.Add(token);
                    continue;
                }

                if (token.Text == ")")
                {
                    if (depth == 0)
                    {
                        args.Add(current);
                        close = i;
                        return args;
                    }

                    depth--;
                    current.Add(token);
                    continue;
                }

                if (token.Text == "," && depth == 0)
                {
                    args.Add(current);
                    current = new List<Token>();
                    continue;
                }

                current.Add(token);
            }

            throw new GlslPreprocessorException(fileName, lineNumber, "Unterminated macro invocation.");
        }

        private void UpdateVersion(string argument, string fileName, int lineNumber)
        {
            int p = 0;
            SkipHorizontalWhitespace(argument, ref p);
            int start = p;

            while (p < argument.Length && char.IsDigit(argument[p]))
                p++;

            if (start == p || !int.TryParse(argument[start..p], NumberStyles.None, CultureInfo.InvariantCulture, out _version))
                throw new GlslPreprocessorException(fileName, lineNumber, "Invalid #version directive.");
        }

        private void EmitBranchStart(StringBuilder result, ConditionalFrame frame)
        {
            if (!_owner.EmitConditionComments || !frame.EmitDebug)
                return;

            if (frame.CurrentActive)
                result.Append("// [preprocessor begin] ").Append(frame.BranchText).Append('\n');
            else
                result.Append("// [preprocessor removed] ").Append(frame.BranchText).Append('\n');
        }

        private void EmitBranchEnd(StringBuilder result, ConditionalFrame frame)
        {
            if (!_owner.EmitConditionComments || !frame.EmitDebug || !frame.CurrentActive)
                return;

            result.Append("// [preprocessor end] ").Append(frame.BranchText).Append('\n');
        }
    }

    private sealed record Macro(string Name, List<string>? Parameters, string Body);

    private readonly record struct ConditionalFrame(
        bool ParentActive,
        bool CurrentActive,
        bool AnyTaken,
        string BranchText,
        bool EmitDebug,
        bool SeenElse = false,
        bool IsRuntime = false,
        bool EmitRuntimeControlFlow = false);

    private sealed class GlslScopeTracker
    {
        private readonly Stack<bool> _braces = new();
        private string? _previousToken;
        private int _functionDepth;

        public bool IsInsideFunction => _functionDepth > 0;

        public void Process(string line)
        {
            var tokens = Tokenize(line);

            foreach (var token in tokens)
            {
                if (token.Kind == TokenKind.WhiteSpace)
                    continue;

                if (token.Text == "{")
                {
                    bool isFunction = _functionDepth == 0 && _previousToken == ")";
                    _braces.Push(isFunction);
                    if (isFunction)
                        _functionDepth++;
                }
                else if (token.Text == "}")
                {
                    if (_braces.Count > 0 && _braces.Pop())
                        _functionDepth--;
                }

                _previousToken = token.Text;
            }
        }
    }

    private readonly record struct LogicalLine(string Text, int LineNumber);

    private enum TokenKind
    {
        Identifier,
        Number,
        WhiteSpace,
        String,
        Punctuator
    }

    private readonly record struct Token(TokenKind Kind, string Text);

    private static IEnumerable<LogicalLine> ReadLogicalLines(string source)
    {
        int physicalLine = 1;
        int start = 0;
        var current = new StringBuilder();
        int logicalStart = 1;

        for (int i = 0; i <= source.Length; i++)
        {
            bool end = i == source.Length;
            if (end && start == source.Length)
                break;

            if (!end && source[i] != '\r' && source[i] != '\n')
                continue;

            string part = source[start..i];
            int newlineLength = 0;

            if (!end)
            {
                newlineLength = 1;
                if (source[i] == '\r' && i + 1 < source.Length && source[i + 1] == '\n')
                    newlineLength = 2;
            }

            bool continued = !end && part.EndsWith('\\');
            if (continued)
                current.Append(part, 0, part.Length - 1);
            else
                current.Append(part);

            if (!continued)
            {
                yield return new LogicalLine(current.ToString(), logicalStart);
                current.Clear();
                logicalStart = physicalLine + 1;
            }

            if (end)
                break;

            physicalLine++;
            i += newlineLength - 1;
            start = i + 1;
        }
    }

    private static bool TryReadDirective(string line, out string directive, out string argument)
    {
        int p = 0;
        SkipHorizontalWhitespace(line, ref p);

        if (p >= line.Length || line[p] != '#')
        {
            directive = string.Empty;
            argument = string.Empty;
            return false;
        }

        p++;
        SkipHorizontalWhitespace(line, ref p);
        directive = ReadIdentifier(line, ref p);
        argument = p < line.Length ? line[p..].TrimStart(' ', '\t') : string.Empty;
        return true;
    }

    private static string ReplaceCommentsWithSpaces(string line, ref bool inBlockComment)
    {
        var result = new StringBuilder(line.Length);

        for (int i = 0; i < line.Length; i++)
        {
            if (inBlockComment)
            {
                if (i + 1 < line.Length && line[i] == '*' && line[i + 1] == '/')
                {
                    result.Append("  ");
                    i++;
                    inBlockComment = false;
                }
                else
                {
                    result.Append(' ');
                }

                continue;
            }

            if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/')
            {
                result.Append(' ', line.Length - i);
                break;
            }

            if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '*')
            {
                result.Append("  ");
                i++;
                inBlockComment = true;
                continue;
            }

            result.Append(line[i]);
        }

        return result.ToString();
    }

    private static List<Token> Tokenize(string text)
    {
        var result = new List<Token>();

        for (int i = 0; i < text.Length;)
        {
            char c = text[i];

            if (char.IsWhiteSpace(c))
            {
                int start = i++;
                while (i < text.Length && char.IsWhiteSpace(text[i]))
                    i++;
                result.Add(new Token(TokenKind.WhiteSpace, text[start..i]));
                continue;
            }

            if (IsIdentifierStart(c))
            {
                int start = i++;
                while (i < text.Length && IsIdentifierPart(text[i]))
                    i++;
                result.Add(new Token(TokenKind.Identifier, text[start..i]));
                continue;
            }

            if (char.IsDigit(c))
            {
                int start = i++;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || text[i] == '.'))
                    i++;
                result.Add(new Token(TokenKind.Number, text[start..i]));
                continue;
            }

            if (c == '"')
            {
                int start = i++;
                while (i < text.Length && text[i] != '"')
                    i++;
                if (i < text.Length)
                    i++;
                result.Add(new Token(TokenKind.String, text[start..i]));
                continue;
            }

            string? op = null;
            if (i + 1 < text.Length)
            {
                string two = text.Substring(i, 2);
                if (two is "##" or "&&" or "||" or "<<" or ">>" or "<=" or ">=" or "==" or "!=")
                    op = two;
            }

            if (op != null)
            {
                result.Add(new Token(TokenKind.Punctuator, op));
                i += 2;
            }
            else
            {
                result.Add(new Token(TokenKind.Punctuator, c.ToString()));
                i++;
            }
        }

        return result;
    }

    private static string Join(IEnumerable<Token> tokens)
    {
        var result = new StringBuilder();
        foreach (var token in tokens)
            result.Append(token.Text);
        return result.ToString();
    }

    private static List<Token> CloneTokens(IEnumerable<Token> tokens) => tokens.ToList();

    private static int NextSignificant(IReadOnlyList<Token> tokens, int start)
    {
        for (int i = start; i < tokens.Count; i++)
        {
            if (tokens[i].Kind != TokenKind.WhiteSpace)
                return i;
        }

        return -1;
    }

    private static int PreviousSignificant(IReadOnlyList<Token> tokens, int start)
    {
        for (int i = start; i >= 0; i--)
        {
            if (tokens[i].Kind != TokenKind.WhiteSpace)
                return i;
        }

        return -1;
    }

    private static bool IsAllWhitespace(IEnumerable<Token> tokens) => tokens.All(t => t.Kind == TokenKind.WhiteSpace);

    private static string ParseSingleIdentifier(string argument, string fileName, int lineNumber, string directive)
    {
        int p = 0;
        SkipHorizontalWhitespace(argument, ref p);
        string name = ReadIdentifier(argument, ref p);
        SkipHorizontalWhitespace(argument, ref p);

        if (name.Length == 0 || p != argument.Length)
            throw new GlslPreprocessorException(fileName, lineNumber, $"{directive} requires exactly one identifier.");

        return name;
    }

    private static string ReadIdentifier(string text, ref int p)
    {
        if (p >= text.Length || !IsIdentifierStart(text[p]))
            return string.Empty;

        int start = p++;
        while (p < text.Length && IsIdentifierPart(text[p]))
            p++;
        return text[start..p];
    }

    private static bool IsIdentifierStart(char c) => c == '_' || char.IsLetter(c);

    private static bool IsIdentifierPart(char c) => c == '_' || char.IsLetterOrDigit(c);

    private static void SkipHorizontalWhitespace(string text, ref int p)
    {
        while (p < text.Length && (text[p] == ' ' || text[p] == '\t'))
            p++;
    }

    private sealed class ExpressionParser
    {
        private readonly List<Token> _tokens;
        private readonly string _fileName;
        private readonly int _lineNumber;
        private int _p;

        public ExpressionParser(string expression, string fileName, int lineNumber)
        {
            _tokens = Tokenize(expression).Where(t => t.Kind != TokenKind.WhiteSpace).ToList();
            _fileName = fileName;
            _lineNumber = lineNumber;
        }

        public long Parse()
        {
            long value = ParseLogicalOr(true);
            if (_p != _tokens.Count)
                Error($"Unexpected token '{_tokens[_p].Text}' in preprocessor expression.");
            return value;
        }

        private long ParseLogicalOr(bool eval)
        {
            long left = ParseLogicalAnd(eval);
            while (Match("||"))
            {
                bool leftTrue = eval && left != 0;
                long right = ParseLogicalAnd(eval && !leftTrue);
                if (eval)
                    left = leftTrue || right != 0 ? 1 : 0;
            }
            return left;
        }

        private long ParseLogicalAnd(bool eval)
        {
            long left = ParseBitwiseOr(eval);
            while (Match("&&"))
            {
                bool leftFalse = eval && left == 0;
                long right = ParseBitwiseOr(eval && !leftFalse);
                if (eval)
                    left = !leftFalse && right != 0 ? 1 : 0;
            }
            return left;
        }

        private long ParseBitwiseOr(bool eval)
        {
            long left = ParseBitwiseXor(eval);
            while (Match("|"))
            {
                long right = ParseBitwiseXor(eval);
                if (eval)
                    left |= right;
            }
            return left;
        }

        private long ParseBitwiseXor(bool eval)
        {
            long left = ParseBitwiseAnd(eval);
            while (Match("^"))
            {
                long right = ParseBitwiseAnd(eval);
                if (eval)
                    left ^= right;
            }
            return left;
        }

        private long ParseBitwiseAnd(bool eval)
        {
            long left = ParseEquality(eval);
            while (Match("&"))
            {
                long right = ParseEquality(eval);
                if (eval)
                    left &= right;
            }
            return left;
        }

        private long ParseEquality(bool eval)
        {
            long left = ParseRelational(eval);
            while (true)
            {
                if (Match("=="))
                {
                    long right = ParseRelational(eval);
                    if (eval)
                        left = left == right ? 1 : 0;
                }
                else if (Match("!="))
                {
                    long right = ParseRelational(eval);
                    if (eval)
                        left = left != right ? 1 : 0;
                }
                else
                {
                    return left;
                }
            }
        }

        private long ParseRelational(bool eval)
        {
            long left = ParseShift(eval);
            while (true)
            {
                if (Match("<"))
                {
                    long right = ParseShift(eval);
                    if (eval)
                        left = left < right ? 1 : 0;
                }
                else if (Match(">"))
                {
                    long right = ParseShift(eval);
                    if (eval)
                        left = left > right ? 1 : 0;
                }
                else if (Match("<="))
                {
                    long right = ParseShift(eval);
                    if (eval)
                        left = left <= right ? 1 : 0;
                }
                else if (Match(">="))
                {
                    long right = ParseShift(eval);
                    if (eval)
                        left = left >= right ? 1 : 0;
                }
                else
                {
                    return left;
                }
            }
        }

        private long ParseShift(bool eval)
        {
            long left = ParseAdditive(eval);
            while (true)
            {
                if (Match("<<"))
                {
                    long right = ParseAdditive(eval);
                    if (eval)
                        left = unchecked(left << (int)right);
                }
                else if (Match(">>"))
                {
                    long right = ParseAdditive(eval);
                    if (eval)
                        left >>= (int)right;
                }
                else
                {
                    return left;
                }
            }
        }

        private long ParseAdditive(bool eval)
        {
            long left = ParseMultiplicative(eval);
            while (true)
            {
                if (Match("+"))
                {
                    long right = ParseMultiplicative(eval);
                    if (eval)
                        left = unchecked(left + right);
                }
                else if (Match("-"))
                {
                    long right = ParseMultiplicative(eval);
                    if (eval)
                        left = unchecked(left - right);
                }
                else
                {
                    return left;
                }
            }
        }

        private long ParseMultiplicative(bool eval)
        {
            long left = ParseUnary(eval);
            while (true)
            {
                if (Match("*"))
                {
                    long right = ParseUnary(eval);
                    if (eval)
                        left = unchecked(left * right);
                }
                else if (Match("/"))
                {
                    long right = ParseUnary(eval);
                    if (eval)
                    {
                        if (right == 0)
                            Error("Division by zero in preprocessor expression.");
                        left /= right;
                    }
                }
                else if (Match("%"))
                {
                    long right = ParseUnary(eval);
                    if (eval)
                    {
                        if (right == 0)
                            Error("Modulo by zero in preprocessor expression.");
                        left %= right;
                    }
                }
                else
                {
                    return left;
                }
            }
        }

        private long ParseUnary(bool eval)
        {
            if (Match("+"))
                return ParseUnary(eval);

            if (Match("-"))
            {
                long value = ParseUnary(eval);
                return eval ? unchecked(-value) : 0;
            }

            if (Match("!"))
            {
                long value = ParseUnary(eval);
                return eval ? value == 0 ? 1 : 0 : 0;
            }

            if (Match("~"))
            {
                long value = ParseUnary(eval);
                return eval ? ~value : 0;
            }

            return ParsePrimary(eval);
        }

        private long ParsePrimary(bool eval)
        {
            if (Match("("))
            {
                long value = ParseLogicalOr(eval);
                Expect(")");
                return value;
            }

            if (_p >= _tokens.Count)
                Error("Unexpected end of preprocessor expression.");

            var token = _tokens[_p++];

            if (token.Kind == TokenKind.Identifier)
                return 0;

            if (token.Kind != TokenKind.Number)
                Error($"Integer literal expected, got '{token.Text}'.");

            return eval ? ParseInteger(token.Text) : 0;
        }

        private static long ParseInteger(string text)
        {
            string value = text;
            while (value.Length > 0 && value[^1] is 'u' or 'U' or 'l' or 'L')
                value = value[..^1];

            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return unchecked((long)ulong.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));

            if (value.Length > 1 && value[0] == '0')
            {
                ulong result = 0;
                for (int i = 1; i < value.Length; i++)
                {
                    if (value[i] < '0' || value[i] > '7')
                        throw new FormatException($"Invalid octal integer literal '{text}'.");
                    result = unchecked(result * 8 + (uint)(value[i] - '0'));
                }
                return unchecked((long)result);
            }

            return unchecked((long)ulong.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture));
        }

        private bool Match(string token)
        {
            if (_p >= _tokens.Count || _tokens[_p].Text != token)
                return false;
            _p++;
            return true;
        }

        private void Expect(string token)
        {
            if (!Match(token))
                Error($"Expected '{token}'.");
        }

        private void Error(string message)
        {
            throw new GlslPreprocessorException(_fileName, _lineNumber, message);
        }
    }
}

public sealed class GlslPreprocessorException : Exception
{
    public GlslPreprocessorException(string fileName, int lineNumber, string message)
        : base(Format(fileName, lineNumber, message))
    {
        FileName = fileName;
        LineNumber = lineNumber;
    }

    public GlslPreprocessorException(string fileName, int lineNumber, string message, Exception innerException)
        : base(Format(fileName, lineNumber, message), innerException)
    {
        FileName = fileName;
        LineNumber = lineNumber;
    }

    public string FileName { get; }

    public int LineNumber { get; }

    private static string Format(string fileName, int lineNumber, string message)
    {
        return lineNumber > 0 ? $"{fileName}({lineNumber}): {message}" : $"{fileName}: {message}";
    }
}
