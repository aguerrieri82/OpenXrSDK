using System.Reflection;
using System.Text;

namespace WrapperGen;

public static class Program
{
    private enum Backend
    {
        OpenGL,
        OpenGLES
    }

    private sealed record MethodEntry(
        MethodInfo Method,
        bool IsExtension,
        ParameterInfo[] PublicParameters,
        Backend Backend);

    private sealed record GeneratedMethod(
        MethodEntry OpenGL,
        MethodEntry? OpenGLES)
    {
        public bool IsOpenGLOnly => OpenGLES == null;
    }

    public static void Main(string[] args)
    {
        var outputDir =
            args.Length > 0
                ? Path.GetFullPath(args[0])
                : Directory.GetCurrentDirectory();

        Directory.CreateDirectory(outputDir);

        var methods = CollectGeneratedMethods();

        var contextType =
            typeof(Silk.NET.OpenGL.GL)
                .GetProperty("Context", BindingFlags.Instance | BindingFlags.Public)
                ?.PropertyType;

        File.WriteAllText(
            Path.Combine(outputDir, "IGLWrapper.generated.cs"),
            GenerateInterface(methods, contextType));

        File.WriteAllText(
            Path.Combine(outputDir, "GLWrapper.generated.cs"),
            GenerateQueuedWrapper(methods, contextType));

        File.WriteAllText(
            Path.Combine(outputDir, "GLDirectWrapper.generated.cs"),
            GenerateDirectWrapper(methods, contextType));

        File.WriteAllText(
            Path.Combine(outputDir, "GLForwardWrapper.generated.cs"),
            GenerateForwardWrapper(methods, contextType));

        Console.WriteLine($"Generated {methods.Length} GL wrapper methods into:");
        Console.WriteLine(outputDir);
    }

    private static GeneratedMethod[] CollectGeneratedMethods()
    {
        var glEntries = CollectBackendMethods(
            typeof(Silk.NET.OpenGL.GL),
            typeof(Silk.NET.OpenGL.GLOverloads),
            Backend.OpenGL);

        var glesEntries = CollectBackendMethods(
            typeof(Silk.NET.OpenGLES.GL),
            typeof(Silk.NET.OpenGLES.GLOverloads),
            Backend.OpenGLES);

        var glesByKey = glesEntries
            .GroupBy(GetPublicSignatureKey)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var result = new List<GeneratedMethod>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var gl in glEntries)
        {
            var key = GetPublicSignatureKey(gl);

            if (!seen.Add(key))
                continue;

            if (glesByKey.TryGetValue(key, out var gles) &&
                HaveSameEmittedSignature(gl, gles))
            {
                result.Add(new GeneratedMethod(gl, gles));
            }
            else
            {
                result.Add(new GeneratedMethod(gl, null));
            }
        }

        return result
            .OrderBy(e => e.OpenGL.Method.Name, StringComparer.Ordinal)
            .ThenBy(e => e.OpenGL.Method.GetGenericArguments().Length)
            .ThenBy(e => GetPublicSignatureKey(e.OpenGL), StringComparer.Ordinal)
            .ToArray();
    }

    private static MethodEntry[] CollectBackendMethods(
        Type glType,
        Type overloadsType,
        Backend backend)
    {
        var entries = new List<MethodEntry>();

        foreach (var method in glType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName))
        {
            entries.Add(new MethodEntry(
                Method: method,
                IsExtension: false,
                PublicParameters: method.GetParameters(),
                Backend: backend));
        }

        foreach (var method in overloadsType
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => !m.IsSpecialName))
        {
            var ps = method.GetParameters();

            if (ps.Length == 0)
                continue;

            var firstType = ps[0].ParameterType;

            if (firstType.IsByRef)
                firstType = firstType.GetElementType()!;

            if (firstType != glType)
                continue;

            entries.Add(new MethodEntry(
                Method: method,
                IsExtension: true,
                PublicParameters: ps.Skip(1).ToArray(),
                Backend: backend));
        }

        return entries
            .OrderBy(e => e.Method.Name, StringComparer.Ordinal)
            .ThenBy(e => e.IsExtension ? 1 : 0)
            .ThenBy(e => e.Method.MetadataToken)
            .GroupBy(GetPublicSignatureKey)
            .Select(g => g.First())
            .ToArray();
    }

    private static string GenerateInterface(GeneratedMethod[] methods, Type? contextType)
    {
        var sb = new StringBuilder(1024 * 1024);

        EmitHeader(sb);

        sb.AppendLine("public unsafe partial interface IGLWrapper");
        sb.AppendLine("{");

        if (contextType != null)
        {
            sb.AppendLine($"    {GetTypeName(contextType)} Context {{ get; }}");
            sb.AppendLine();
        }

        foreach (var entry in methods)
        {
            try
            {
                EmitInterfaceMethod(sb, entry.OpenGL);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"    // FAILED: {entry.OpenGL.Method.Name} - {ex.GetType().Name}: {ex.Message}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateQueuedWrapper(GeneratedMethod[] methods, Type? contextType)
    {
        var sb = new StringBuilder(1024 * 1024);

        EmitHeader(sb);

        sb.AppendLine("public unsafe partial class GLWrapper : global::XrEngine.Wrapper<GL>, IGLWrapper");
        sb.AppendLine("{");
        sb.AppendLine("    public GLWrapper(GL instance)");
        sb.AppendLine("        : base(instance)");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (contextType != null)
        {
            sb.AppendLine($"    public {GetTypeName(contextType)} Context => _instance.Context;");
            sb.AppendLine();
        }

        foreach (var entry in methods)
        {
            try
            {
                EmitQueuedMethod(sb, entry);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"    // FAILED: {entry.OpenGL.Method.Name} - {ex.GetType().Name}: {ex.Message}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateDirectWrapper(GeneratedMethod[] methods, Type? contextType)
    {
        var sb = new StringBuilder(1024 * 1024);

        EmitHeader(sb);

        sb.AppendLine("public unsafe partial class GLDirectWrapper : IGLWrapper");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly GL _instance;");
        sb.AppendLine();
        sb.AppendLine("    public GLDirectWrapper(GL instance)");
        sb.AppendLine("    {");
        sb.AppendLine("        _instance = instance;");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (contextType != null)
        {
            sb.AppendLine($"    public {GetTypeName(contextType)} Context => _instance.Context;");
            sb.AppendLine();
        }

        foreach (var entry in methods)
        {
            try
            {
                EmitDirectMethod(sb, entry);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"    // FAILED: {entry.OpenGL.Method.Name} - {ex.GetType().Name}: {ex.Message}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void EmitHeader(StringBuilder sb)
    {
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS8625");
        sb.AppendLine();
        sb.AppendLine("#if GLES");
        sb.AppendLine("using Silk.NET.OpenGLES;");
        sb.AppendLine("using Buffer = Silk.NET.OpenGLES.Buffer;");
        sb.AppendLine("using Shader = Silk.NET.OpenGLES.Shader;");
        sb.AppendLine("#else");
        sb.AppendLine("using Silk.NET.OpenGL;");
        sb.AppendLine("using Buffer = Silk.NET.OpenGL.Buffer;");
        sb.AppendLine("using Shader = Silk.NET.OpenGL.Shader;");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("namespace OpenGLWrapper;");
        sb.AppendLine();
    }

    private static void EmitInterfaceMethod(StringBuilder sb, MethodEntry entry)
    {
        var method = entry.Method;
        var parameters = entry.PublicParameters;

        sb.Append("    ");
        sb.Append(GetTypeName(method.ReturnType));
        sb.Append(' ');
        sb.Append(method.Name);
        sb.Append(GetGenericDeclaration(method));
        sb.Append('(');
        sb.Append(string.Join(", ", parameters.Select(GetParameterDeclaration)));
        sb.Append(')');

        var constraints = GetGenericConstraints(method).ToArray();

        if (constraints.Length == 0)
        {
            sb.AppendLine(";");
            sb.AppendLine();
            return;
        }

        sb.AppendLine();

        for (var i = 0; i < constraints.Length; i++)
        {
            sb.Append("        ");
            sb.Append(constraints[i]);

            if (i == constraints.Length - 1)
                sb.AppendLine(";");
            else
                sb.AppendLine();
        }

        sb.AppendLine();
    }

    private static void EmitQueuedMethod(StringBuilder sb, GeneratedMethod generated)
    {
        var entry = generated.OpenGL;
        var method = entry.Method;
        var parameters = entry.PublicParameters;

        EmitMethodHeader(sb, entry);

        sb.AppendLine("    {");

        if (generated.IsOpenGLOnly)
        {
            sb.AppendLine("#if GLES");
            sb.AppendLine($"        throw new global::System.NotSupportedException(\"OpenGL-only method '{method.Name}' is not available when GLES is enabled.\");");
            sb.AppendLine("#else");
        }

        foreach (var parameter in parameters)
            EmitCapture(sb, parameter);

        sb.AppendLine("        AddAction(gl =>");
        sb.AppendLine("        {");

        foreach (var parameter in parameters)
            EmitLambdaLocal(sb, parameter);

        sb.Append("            ");

        if (method.ReturnType != typeof(void))
            sb.Append("_ = ");

        EmitInvocationPrefix(sb, entry, "gl");

        sb.Append(GetGenericInvocation(method));
        sb.Append('(');
        sb.Append(string.Join(", ", GetQueuedInvocationArguments(entry)));
        sb.AppendLine(");");

        sb.AppendLine("        });");

        if (method.ReturnType != typeof(void))
            sb.AppendLine("        return default;");

        if (generated.IsOpenGLOnly)
            sb.AppendLine("#endif");

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDirectMethod(StringBuilder sb, GeneratedMethod generated)
    {
        var entry = generated.OpenGL;
        var method = entry.Method;

        EmitMethodHeader(sb, entry);

        sb.AppendLine("    {");

        if (generated.IsOpenGLOnly)
        {
            sb.AppendLine("#if GLES");
            sb.AppendLine($"        throw new global::System.NotSupportedException(\"OpenGL-only method '{method.Name}' is not available when GLES is enabled.\");");
            sb.AppendLine("#else");
        }

        sb.Append("        ");

        if (method.ReturnType != typeof(void))
            sb.Append("return ");

        EmitInvocationPrefix(sb, entry, "_instance");

        sb.Append(GetGenericInvocation(method));
        sb.Append('(');
        sb.Append(string.Join(", ", GetDirectInvocationArguments(entry)));
        sb.AppendLine(");");

        if (generated.IsOpenGLOnly)
            sb.AppendLine("#endif");

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitMethodHeader(StringBuilder sb, MethodEntry entry)
    {
        var method = entry.Method;
        var parameters = entry.PublicParameters;

        sb.Append("    public ");
        sb.Append(GetTypeName(method.ReturnType));
        sb.Append(' ');
        sb.Append(method.Name);
        sb.Append(GetGenericDeclaration(method));
        sb.Append('(');
        sb.Append(string.Join(", ", parameters.Select(GetParameterDeclaration)));
        sb.AppendLine(")");

        foreach (var constraint in GetGenericConstraints(method))
            sb.AppendLine($"        {constraint}");
    }

    private static void EmitInvocationPrefix(StringBuilder sb, MethodEntry entry, string glExpression)
    {
        if (entry.IsExtension)
        {
            sb.Append("GLOverloads.");
            sb.Append(entry.Method.Name);
        }
        else
        {
            sb.Append(glExpression);
            sb.Append('.');
            sb.Append(entry.Method.Name);
        }
    }

    private static IEnumerable<string> GetQueuedInvocationArguments(MethodEntry entry)
    {
        if (entry.IsExtension)
            yield return "gl";

        foreach (var parameter in entry.PublicParameters)
            yield return GetQueuedLambdaArgument(parameter);
    }

    private static IEnumerable<string> GetDirectInvocationArguments(MethodEntry entry)
    {
        if (entry.IsExtension)
            yield return "_instance";

        foreach (var parameter in entry.PublicParameters)
            yield return GetDirectArgument(parameter);
    }

    private static void EmitCapture(StringBuilder sb, ParameterInfo parameter)
    {
        var param = ParamName(parameter);
        var cap = CaptureName(parameter);
        var type = parameter.ParameterType;

        if (parameter.IsOut)
        {
            sb.AppendLine($"        {param} = default;");
            return;
        }

        if (IsSpanLike(type))
        {
            sb.AppendLine($"        var {cap} = {param}.ToArray();");
            return;
        }

        if (type.IsByRef)
        {
            sb.AppendLine($"        var {cap} = {param};");
            return;
        }

        if (type.IsPointer)
        {
            sb.AppendLine($"        var {cap} = (nint){param};");
            return;
        }

        sb.AppendLine($"        var {cap} = {param};");
    }

    private static void EmitLambdaLocal(StringBuilder sb, ParameterInfo parameter)
    {
        var cap = CaptureName(parameter);
        var local = LocalName(parameter);
        var type = parameter.ParameterType;

        if (parameter.IsOut)
        {
            var elementType = GetElementTypeName(type);
            sb.AppendLine($"            {elementType} {local};");
            return;
        }

        if (IsSpanLike(type))
            return;

        if (type.IsByRef)
        {
            sb.AppendLine($"            var {local} = {cap};");
            return;
        }

        if (type.IsPointer)
        {
            sb.AppendLine($"            var {local} = ({GetTypeName(type)}){cap};");
            return;
        }
    }

    private static string GetParameterDeclaration(ParameterInfo parameter)
    {
        var param = ParamName(parameter);
        var type = parameter.ParameterType;

        if (parameter.IsOut)
            return $"out {GetElementTypeName(type)} {param}";

        if (type.IsByRef)
        {
            if (IsReadOnlyRef(parameter))
                return $"in {GetElementTypeName(type)} {param}";

            return $"ref {GetElementTypeName(type)} {param}";
        }

        return $"{GetTypeName(type)} {param}";
    }

    private static string GetQueuedLambdaArgument(ParameterInfo parameter)
    {
        var cap = CaptureName(parameter);
        var local = LocalName(parameter);
        var type = parameter.ParameterType;

        if (parameter.IsOut)
            return $"out {local}";

        if (IsSpanLike(type))
            return cap;

        if (type.IsByRef)
        {
            if (IsReadOnlyRef(parameter))
                return $"in {local}";

            return $"ref {local}";
        }

        if (type.IsPointer)
            return local;

        return cap;
    }

    private static string GetDirectArgument(ParameterInfo parameter)
    {
        var param = ParamName(parameter);
        var type = parameter.ParameterType;

        if (parameter.IsOut)
            return $"out {param}";

        if (type.IsByRef)
        {
            if (IsReadOnlyRef(parameter))
                return $"in {param}";

            return $"ref {param}";
        }

        return param;
    }

    private static string GetPublicSignatureKey(MethodEntry entry)
    {
        var sb = new StringBuilder();

        sb.Append(entry.Method.Name);
        sb.Append('`');
        sb.Append(entry.Method.GetGenericArguments().Length);
        sb.Append('(');

        foreach (var parameter in entry.PublicParameters)
        {
            sb.Append(GetNormalizedTypeKey(parameter.ParameterType));
            sb.Append('|');

            if (parameter.IsOut)
                sb.Append("out");
            else if (parameter.ParameterType.IsByRef)
                sb.Append(IsReadOnlyRef(parameter) ? "in" : "ref");
            else
                sb.Append("val");

            sb.Append(';');
        }

        sb.Append(')');
        return sb.ToString();
    }

    private static bool HaveSameEmittedSignature(MethodEntry a, MethodEntry b)
    {
        if (a.Method.Name != b.Method.Name)
            return false;

        if (a.Method.GetGenericArguments().Length != b.Method.GetGenericArguments().Length)
            return false;

        if (GetTypeName(a.Method.ReturnType) != GetTypeName(b.Method.ReturnType))
            return false;

        if (a.PublicParameters.Length != b.PublicParameters.Length)
            return false;

        for (var i = 0; i < a.PublicParameters.Length; i++)
        {
            var pa = a.PublicParameters[i];
            var pb = b.PublicParameters[i];

            if (pa.IsOut != pb.IsOut)
                return false;

            if (pa.ParameterType.IsByRef != pb.ParameterType.IsByRef)
                return false;

            if (pa.ParameterType.IsByRef && IsReadOnlyRef(pa) != IsReadOnlyRef(pb))
                return false;

            if (GetTypeName(pa.ParameterType) != GetTypeName(pb.ParameterType))
                return false;
        }

        return true;
    }

    private static string GetNormalizedTypeKey(Type type)
    {
        if (type.IsByRef)
            return GetNormalizedTypeKey(type.GetElementType()!) + "&";

        if (type.IsPointer)
            return GetNormalizedTypeKey(type.GetElementType()!) + "*";

        if (type.IsGenericParameter)
            return "!" + type.GenericParameterPosition;

        if (type.IsArray)
            return GetNormalizedTypeKey(type.GetElementType()!) + "[]";

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            var defKey = GetNormalizedNonGenericTypeKey(def);
            var args = string.Join(",", type.GetGenericArguments().Select(GetNormalizedTypeKey));
            return defKey + "<" + args + ">";
        }

        return GetNormalizedNonGenericTypeKey(type);
    }

    private static string GetNormalizedNonGenericTypeKey(Type type)
    {
        var ns = type.Namespace ?? "";
        var name = type.Name;

        var tickIndex = name.IndexOf('`');
        if (tickIndex >= 0)
            name = name[..tickIndex];

        if (ns is "Silk.NET.OpenGL" or "Silk.NET.OpenGLES")
            return "Silk.NET.GL_BACKEND." + name;

        return ns + "." + name;
    }

    private static bool IsReadOnlyRef(ParameterInfo parameter)
    {
        if (!parameter.ParameterType.IsByRef)
            return false;

        if (parameter.IsIn && !parameter.IsOut)
            return true;

        return parameter
            .GetRequiredCustomModifiers()
            .Any(t => t.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute");
    }

    private static bool IsSpanLike(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var def = type.GetGenericTypeDefinition();

        return
            def == typeof(Span<>) ||
            def == typeof(ReadOnlySpan<>);
    }

    private static string GetGenericDeclaration(MethodInfo method)
    {
        if (!method.IsGenericMethodDefinition)
            return "";

        return "<" + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ">";
    }

    private static string GetGenericInvocation(MethodInfo method)
    {
        if (!method.IsGenericMethodDefinition)
            return "";

        return "<" + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ">";
    }

    private static IEnumerable<string> GetGenericConstraints(MethodInfo method)
    {
        if (!method.IsGenericMethodDefinition)
            yield break;

        foreach (var arg in method.GetGenericArguments())
        {
            var attrs = arg.GenericParameterAttributes;
            var special = attrs & GenericParameterAttributes.SpecialConstraintMask;

            var constraints = new List<string>();

            var hasUnmanaged =
                arg.GetCustomAttributesData()
                    .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.IsUnmanagedAttribute");

            if (hasUnmanaged)
            {
                constraints.Add("unmanaged");
            }
            else
            {
                if ((special & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                    constraints.Add("class");

                if ((special & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                    constraints.Add("struct");
            }

            foreach (var constraint in arg.GetGenericParameterConstraints())
            {
                if (ShouldSkipConstraintType(constraint, hasUnmanaged))
                    continue;

                constraints.Add(GetTypeName(constraint));
            }

            if ((special & GenericParameterAttributes.DefaultConstructorConstraint) != 0 &&
                !constraints.Contains("struct") &&
                !constraints.Contains("unmanaged"))
            {
                constraints.Add("new()");
            }

            if (constraints.Count != 0)
                yield return $"where {arg.Name} : {string.Join(", ", constraints.Distinct())}";
        }
    }

    private static bool ShouldSkipConstraintType(Type type, bool hasUnmanaged)
    {
        return hasUnmanaged && type == typeof(ValueType);
    }

    private static string GetElementTypeName(Type type)
    {
        if (type.IsByRef || type.IsPointer)
            return GetTypeName(type.GetElementType()!);

        return GetTypeName(type);
    }

    private static string GetTypeName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(short)) return "short";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(int)) return "int";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(long)) return "long";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(float)) return "float";
        if (type == typeof(double)) return "double";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(char)) return "char";
        if (type == typeof(string)) return "string";
        if (type == typeof(object)) return "object";
        if (type == typeof(nint)) return "nint";
        if (type == typeof(nuint)) return "nuint";

        if (type.IsPointer)
            return GetTypeName(type.GetElementType()!) + "*";

        if (type.IsByRef)
            return GetTypeName(type.GetElementType()!);

        if (type.IsGenericParameter)
            return type.Name;

        if (type.IsArray)
            return GetTypeName(type.GetElementType()!) + "[]";

        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            var genericName = GetNonGenericTypeName(genericTypeDef);

            var args = string.Join(", ", type.GetGenericArguments().Select(GetTypeName));
            return $"{genericName}<{args}>";
        }

        return GetNonGenericTypeName(type);
    }

    private static string GetNonGenericTypeName(Type type)
    {
        var ns = type.Namespace ?? "";
        var name = type.Name.Replace('+', '.');

        var tickIndex = name.IndexOf('`');
        if (tickIndex >= 0)
            name = name[..tickIndex];

        if (ns is "Silk.NET.OpenGL" or "Silk.NET.OpenGLES")
            return name;

        if (string.IsNullOrEmpty(ns))
            return name;

        return "global::" + ns + "." + name;
    }

    private static string ParamName(ParameterInfo parameter)
    {
        var raw = RawParamName(parameter);

        if (CSharpKeywords.Contains(raw))
            return "@" + raw;

        return raw;
    }

    private static string CaptureName(ParameterInfo parameter)
    {
        return "__" + RawParamName(parameter);
    }

    private static string LocalName(ParameterInfo parameter)
    {
        var raw = RawParamName(parameter);

        if (raw.Length == 0)
            return "localArg";

        return "local" + char.ToUpperInvariant(raw[0]) + raw[1..];
    }

    private static string RawParamName(ParameterInfo parameter)
    {
        var name = parameter.Name;

        if (string.IsNullOrWhiteSpace(name))
            return "arg" + parameter.Position;

        return SanitizeIdentifier(name);
    }

    private static string SanitizeIdentifier(string name)
    {
        var sb = new StringBuilder(name.Length + 1);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (i == 0)
            {
                if (char.IsLetter(c) || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }
            else
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }
        }

        if (sb.Length == 0)
            return "arg";

        return sb.ToString();
    }

    private static string GenerateForwardWrapper(GeneratedMethod[] methods, Type? contextType)
    {
        var sb = new StringBuilder(1024 * 1024);

        EmitHeader(sb);

        sb.AppendLine("public unsafe partial class GLForwardWrapper : IGLWrapper");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IGLWrapper _instance;");
        sb.AppendLine();
        sb.AppendLine("    public GLForwardWrapper(IGLWrapper instance)");
        sb.AppendLine("    {");
        sb.AppendLine("        _instance = instance;");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (contextType != null)
        {
            sb.AppendLine($"    public {GetTypeName(contextType)} Context => _instance.Context;");
            sb.AppendLine();
        }

        foreach (var entry in methods)
        {
            try
            {
                EmitForwardMethod(sb, entry);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"    // FAILED: {entry.OpenGL.Method.Name} - {ex.GetType().Name}: {ex.Message}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void EmitForwardMethod(StringBuilder sb, GeneratedMethod generated)
    {
        var entry = generated.OpenGL;
        var method = entry.Method;
        var parameters = entry.PublicParameters;

        EmitMethodHeader(sb, entry);

        sb.AppendLine("    {");
        sb.Append("        ");

        if (method.ReturnType != typeof(void))
            sb.Append("return ");

        sb.Append("_instance.");
        sb.Append(method.Name);
        sb.Append(GetGenericInvocation(method));
        sb.Append('(');
        sb.Append(string.Join(", ", parameters.Select(GetForwardArgument)));
        sb.AppendLine(");");

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string GetForwardArgument(ParameterInfo parameter)
    {
        var param = ParamName(parameter);
        var type = parameter.ParameterType;

        if (parameter.IsOut)
            return $"out {param}";

        if (type.IsByRef)
        {
            if (IsReadOnlyRef(parameter))
                return $"in {param}";

            return $"ref {param}";
        }

        return param;
    }

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
        "char", "checked", "class", "const", "continue", "decimal", "default",
        "delegate", "do", "double", "else", "enum", "event", "explicit",
        "extern", "false", "finally", "fixed", "float", "for", "foreach",
        "goto", "if", "implicit", "in", "int", "interface", "internal",
        "is", "lock", "long", "namespace", "new", "null", "object",
        "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong",
        "unchecked", "unsafe", "ushort", "using", "virtual", "void",
        "volatile", "while"
    };
}