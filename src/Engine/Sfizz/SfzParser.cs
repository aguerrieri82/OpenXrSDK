using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sfizz
{
    public class SfzParser
    {
        Dictionary<string, Token[]> _includes = [];
        Dictionary<string, string> _defines = [];
        HashSet<string> _samples = [];
        private string _rootFile;
        string? _basePath;
        private List<Section> _sections = [];
        private Section? _curSection;
        private string? _samplesPath;
        private ILogger _logger;

        public enum TokenType
        {
            Tag,
            Comment,
            Var,
            String,
            Preprocessor,
            Identifier,
            Punctuator,
            Number,
            Whitespace,
            Eof
        }

        public class Token
        {
            public string? Value;

            public int Line;

            public int Column;

            public int Pos;

            public TokenType Type;

            public string? FileName;
        }


        public class Section
        {
            public string? Name;

            public Dictionary<string, string>? Properties;
        }

        public SfzParser()
            : this(NullLogger.Instance)
        {

        }

        public SfzParser(ILogger logger)
        {
            _logger = logger;

        }

        public IEnumerable<Token> Tokenize(string data, string fileName)
        {
            int state = 0;

            int i = 0;
            int line = 0;
            int col = 0;
            var curText = new StringBuilder();

            Token CreateToken(TokenType type)
            {
                var result = new Token
                {
                    Value = curText.ToString(),
                    Line = line,
                    Column = col,
                    Pos = i,
                    Type = type,
                    FileName = fileName
                };
                curText.Clear();
                return result;
            }

            while (i <= data.Length)
            {
                var c = i == data.Length ? (char)0 : data[i];

                if (c == '\r')
                {
                    i++;
                    continue;
                }


                switch (state)
                {
                    case 0:
                        if (c == '<')
                        {
                            state = 1;
                        }
                        else if (c == '/')
                        {
                            state = 2;
                        }
                        else if (c == '$')
                        {
                            state = 4;
                        }
                        else if (c == '#')
                        {
                            state = 5;
                        }
                        else if (c == '"')
                        {
                            state = 6;
                        }
                        else if (char.IsLetter(c) || c == '_')
                        {
                            curText.Append(c);
                            state = 7;
                        }
                        else if (char.IsNumber(c))
                        {
                            curText.Append(c);
                            state = 8;
                        }
                        else if (c == '-' || c == '+')
                        {
                            curText.Append(c);
                            state = 81;
                        }
                        else if (char.IsPunctuation(c) || c == '=')
                        {
                            curText.Append(c);
                            yield return CreateToken(TokenType.Punctuator);
                        }
                        else if (char.IsWhiteSpace(c))
                        {
                            curText.Append(c);
                            state = 9;
                        }
                        else if (c == 0)
                        {

                        }
                        else
                            throw new Exception("Not supported");
                        break;
                    //Region
                    case 1:
                        if (c == '>')
                        {
                            yield return CreateToken(TokenType.Tag);
                            state = 0;
                        }
                        else
                            curText.Append(c);
                        break;
                    //Begin comment
                    case 2:
                        if (c == '/')
                            state = 3;
                        else
                        {
                            curText.Append('/');
                            state = 7;
                            continue;
                        }
                        break;
                    //Comment
                    case 3:
                        if (c == '\n' || c == 0)
                        {
                            yield return CreateToken(TokenType.Comment);
                            state = 0;
                            continue;
                        }
                        else
                            curText.Append(c);

                        break;
                    //Var
                    case 4:
                        if (c == '_')
                            state = 41;

                        else if (!char.IsLetterOrDigit(c))
                        {

                            yield return CreateToken(TokenType.Var);
                            state = 0;
                            continue;
                        }
                        else
                            curText.Append(c);
                        break;
                    //Check underscore var
                    case 41:
                        if (char.IsLetterOrDigit(c))
                        {
                            curText.Append('_');
                            curText.Append(c);
                            state = 4;
                        }
                        else
                        {
                            yield return CreateToken(TokenType.Var);
                            state = 0;
                            i--;
                            continue;
                        }
                        break;
                    //Preprocessor  
                    case 5:
                        if (char.IsWhiteSpace(c) || c == 0)
                        {
                            yield return CreateToken(TokenType.Preprocessor);
                            state = 0;
                            continue;
                        }
                        else
                            curText.Append(c);
                        break;
                    //String  
                    case 6:
                        if (c == '"')
                        {
                            yield return CreateToken(TokenType.String);
                            state = 0;
                        }
                        else
                            curText.Append(c);
                        break;
                    //Identifier  
                    case 7:
                        if (!char.IsWhiteSpace(c) && c != '$' && c != '=' && c != 0)
                            curText.Append(c);
                        else
                        {
                            yield return CreateToken(TokenType.Identifier);
                            state = 0;
                            continue;
                        }
                        break;
                    //Number  
                    case 8:
                        if (c == '.' || char.IsNumber(c))
                            curText.Append(c);
                        else
                        {
                            yield return CreateToken(TokenType.Number);
                            state = 0;
                            continue;
                        }
                        break;
                    //Check sign number 
                    case 81:

                        if (char.IsNumber(c))
                        {
                            curText.Append(c);
                            state = 8;
                        }
                        else
                        {
                            state = 7;
                            continue;
                        }

                        break;
                    //Whitespace    
                    case 9:
                        if (char.IsWhiteSpace(c))
                            curText.Append(c);
                        else
                        {
                            yield return CreateToken(TokenType.Whitespace);
                            state = 0;
                            continue;
                        }
                        break;
                }


                if (c == '\n')
                {
                    line++;
                    col = 0;
                }
                else
                    col++;
                i++;
            }
        }

        public long SamplesSize()
        {
            long result = 0;    
            foreach (var sample in _samples)
                result += new FileInfo(sample).Length;
            return result;
        }

        static string FindCommonBasePath(IEnumerable<string> paths)
        {
            if (paths == null || !paths.Any())
                return string.Empty;

            // Split all paths into directory segments
            var splitPaths = paths.Select(path => Path.GetFullPath(path)
                                                       .TrimEnd(Path.DirectorySeparatorChar)
                                                       .Split(Path.DirectorySeparatorChar))
                                  .ToArray();

            // Find the common segments
            var commonSegments = splitPaths
                .Aggregate((previous, current) => previous.Zip(current, (prev, curr) => prev == curr ? prev : null)
                                                          .TakeWhile(segment => segment != null)
                                                          .Cast<string>()
                                                          .ToArray());

            // Join the common segments back into a path
            return commonSegments.Length > 0
                ? string.Join(Path.DirectorySeparatorChar.ToString(), commonSegments) + Path.DirectorySeparatorChar
                : string.Empty;
        }

        public void CopyTo(string destPath)
        {
            var files = _includes.Keys.Concat(_samples).Distinct();

            var basePath = FindCommonBasePath(files);
            foreach (var file in files)
            {
                var relPath = file.Substring(basePath.Length);
                var newPath = Path.Join(destPath, relPath);
                Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
                File.Copy(file, newPath, true);
            }
        }


        public void Parse(string fileName)
        {
            var fullPath = Path.GetFullPath(fileName);

            if (_basePath == null)
            {
                _rootFile = fileName;
                _basePath = Path.GetDirectoryName(fileName);
            }
     

            if (!_includes.TryGetValue(fullPath, out var tokens))
            {
                tokens = Tokenize(File.ReadAllText(fileName), fileName).ToArray();
                _includes[fileName] = tokens;
            }

            int i = 0;

            Token NextToken(bool ignoreCommentAndSpace = true)
            {
                if (ignoreCommentAndSpace)
                {
                    while (i < tokens.Length)
                    {
                        var curToken = tokens[i++];
                        if (curToken.Type != TokenType.Whitespace && curToken.Type != TokenType.Comment)
                            return curToken;
                    }

                    return new Token() { Type = TokenType.Eof };
                }

                return tokens[i++];
            }


            string ReadValueUntil(Func<Token, bool> cond)
            {
                var result = new StringBuilder();

                while (true)
                {
                    var token = tokens[i];

                    if (token.Type == TokenType.Var)
                    {
                        if (!_defines.TryGetValue(token.Value!, out var value))
                        {
                            _logger.LogWarning("Undefined var {var}", token.Value);
                            value = token.Value;
                        }
                        result.Append(value);
                    }
                    else if (token.Type != TokenType.Comment)
                        result.Append(token.Value);
                    
                    i++;

                    if (i >= tokens.Length || cond(tokens[i]))
                        return result.ToString();

                }
            }

            string FullPath(string path)
            {
                return Path.GetFullPath(Path.Join(_basePath, path.Trim()));
            }

            while (i < tokens.Length)
            {
                var token = NextToken();

                if (token == null)
                    break;

                if (token.Type == TokenType.Preprocessor)
                {
                    if (token.Value == "define")
                    {
                        var name = NextToken();
                   
                        if (name.Type != TokenType.Var)
                            throw new Exception("Expected var");

                        var value = ReadValueUntil(a => a.Type == TokenType.Whitespace).Trim();
                        _defines[name.Value!] = value;

                    }
                    else if (token.Value == "include")
                    {
                        var name = NextToken();
                        var incPath = FullPath(name.Value!);
                        Parse(incPath);
                    }
                    else
                        throw new Exception($"Unknown preprocessor '{token.Value}'");
                }
                else if (token.Type == TokenType.Tag)
                {
                    _curSection = new Section
                    {
                        Name = token.Value,
                        Properties = []
                    };
                    _sections.Add(_curSection);

                }
                else if (token.Type == TokenType.Identifier)
                {
                    if (_curSection?.Properties == null)
                        throw new Exception("Expected section");    

                    i--;

                    var name = ReadValueUntil(a=> a.Type != TokenType.Identifier && a.Type != TokenType.Var).Trim();

                    var op = NextToken();

                    if (op.Value != "=")
                        throw new Exception("Expected '='");

                    var value = ReadValueUntil(a => a.Type == TokenType.Whitespace);

                    _curSection.Properties[name] = value;

                    if (_curSection.Name == "control" && name == "default_path")
                        _samplesPath = FullPath(value);

                    else if (name == "sample")
                    {
                        string path;

                        if (_samplesPath != null)
                            path = Path.Join(_samplesPath, value);
                        else
                            path = FullPath(value);

                        if (value.StartsWith("*"))
                            continue;

                        if (!File.Exists(path))
                            throw new Exception($"Sample '{path}' not found");

                        _samples.Add(path);
                    }
                }
            }
        }
    }
}
