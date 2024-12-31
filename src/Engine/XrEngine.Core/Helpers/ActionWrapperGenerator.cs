using System.Reflection;
using System.Text;


namespace XrEngine
{
    public class ActionWrapperGenerator
    {
        public static string Generate<T>()
        {
            var writer = new StringBuilder();


            void WriteTypeName(Type type)
            {
                if (type.IsArray)
                {
                    writer.Append(type.GetElementType());
                    writer.Append("[]");
                }
                else if (type == typeof(void))
                    writer.Append("void");
                else
                {
                    var name = type.Name;
                    if (name.EndsWith('&'))
                        name = name.Substring(0, name.Length - 1);

                    if (type.IsGenericType)
                        name = name.Split('`')[0];

                    if (type.Namespace != null && type.FullName != null)
                        writer.Append(type.Namespace).Append('.');

                    writer.Append(name);

                    if (type.IsGenericType)
                    {
                        writer.Append("<");
                        var i = 0;
                        foreach (var arg in type.GetGenericArguments())
                        {
                            if (i > 0)
                                writer.Append(", ");
                            WriteTypeName(arg);
                        }
                        writer.Append(">");
                    }
                }
            }

            void WriteConst(object? value)
            {
                if (value == null)
                    writer.Append("null");

                if (value is string str)
                {
                    writer.Append('\"');
                    writer.Append(str.Replace("\"", "\\\""));
                    writer.Append('\"');
                }
                else if (value is char ch)
                {
                    writer.Append('\'');
                    writer.Append(ch);
                    writer.Append('\'');
                }
                else if (value is bool b)
                {
                    writer.Append(b ? "true" : "false");
                }
                else if (value is Enum e)
                {
                    writer.Append(e.GetType().FullName);
                    writer.Append('.');
                    writer.Append(e.ToString());
                }
                else
                {
                    writer.Append(value);
                }
            }

            void WriteRefType(ParameterInfo value)
            {
                var isRef = value.ParameterType.Name.EndsWith('&');
                if (isRef)
                {
                    if (value.IsOut)
                        writer.Append("out ");
                    else if (value.IsIn)
                        writer.Append("in ");
                    else
                        writer.Append("ref ");
                }
            }

            void WriteParameter(ParameterInfo value, bool isReturnType)
            {
                if (value.GetCustomAttribute(typeof(ParamArrayAttribute)) != null)
                    writer.Append("params ");

                WriteRefType(value);

                WriteTypeName(value.ParameterType);

                if (!isReturnType)
                {
                    writer.Append(" ");
                    writer.Append(value.Name);
                    if (value.IsOptional)
                    {
                        writer.Append(" = ");
                        WriteConst(value.DefaultValue);
                    }
                }
            }

            void WriteIdentifier(string value)
            {
                if (value == "string" || value == "params")
                    writer.Append("@");
                writer.Append(value);
            }

            var count = 0;
            foreach (var method in typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod))
            {
                writer.Append("public ");

                WriteParameter(method.ReturnParameter, false);

                writer.Append(" ");

                writer.Append(method.Name);

                var i = 0;

                if (method.IsGenericMethod)
                {
                    writer.Append('<');
                    foreach (var arg in method.GetGenericArguments())
                    {
                        if (i > 0)
                            writer.Append(", ");
                        WriteIdentifier(arg.Name);
                        i++;
                    }
                    writer.Append('>');
                }
                writer.Append("(");
                i = 0;
                foreach (var arg in method.GetParameters())
                {
                    if (i > 0)
                        writer.Append(", ");
                    WriteParameter(arg, false);
                    i++;
                }
                writer.Append(") => AddAction(() => _instance.");
                writer.Append(method.Name);
                writer.Append("(");
                i = 0;
                foreach (var arg in method.GetParameters())
                {
                    if (i > 0)
                        writer.Append(", ");

                    WriteRefType(arg);

                    WriteIdentifier(arg.Name!);

                    i++;
                }
                writer.Append("));");
                writer.AppendLine();
                writer.AppendLine();

                if (count++ > 100)
                    break;
            }

            return writer.ToString();
        }
    }
}
