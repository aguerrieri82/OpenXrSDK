using System.Text;
using System.Text.RegularExpressions;

namespace XrEngine
{
    public static class ShaderPreprocessor
    {
        public enum TokenType
        {
            Define,
            If,
            IfDef,
            IfnDef,
            Undef,
            Else,
            EndIf,
            ElseIf,
            Text,
            Comment,
            Expression
        }

        struct Token
        {
            public TokenType Type;

            public string? Text;

            public override string ToString()
            {
                return $"{Type} {Text}";
            }
        }

        static Token[] Tokenize(string data)
        {
            List<Token> result = new List<Token>();
            StringBuilder curText = new StringBuilder();

            int i = 0;
            int state = 0;

            void AppendIfNotEmpty()
            {
                if (curText.Length > 0)
                    result.Add(new Token
                    {
                        Type = TokenType.Text,
                        Text = curText.ToString()
                    });

                curText.Length = 0;
            }

            while (i < data.Length)
            {
                char c = data[i];

                switch (state)
                {
                    case 0:
                        if (c == '#')
                        {
                            AppendIfNotEmpty();
                            state = 1;
                        }
                        else if (c == '/')
                        {
                            state = 2;
                        }
                        else
                        {
                            curText.Append(c);
                        }

                        break;
                    case 1:
                        if (c == ' ' || c == '\r' || c == '\n')
                        {
                            Token token = new Token();
                            token.Text = curText.ToString();
                            curText.Length = 0;
                            switch (token.Text)
                            {

                                case "define":
                                    token.Type = TokenType.Define;
                                    state = 8;
                                    break;
                                case "undef":
                                    token.Type = TokenType.Undef;
                                    state = 8;
                                    break;
                                case "if":
                                    token.Type = TokenType.If;
                                    state = 8;
                                    break;
                                case "ifdef":
                                    token.Type = TokenType.IfDef;
                                    state = 8;
                                    break;
                                case "ifndef":
                                    token.Type = TokenType.IfnDef;
                                    state = 8;
                                    break;
                                case "elif":
                                    token.Type = TokenType.ElseIf;
                                    state = 8;
                                    break;
                                case "else":
                                    token.Type = TokenType.Else;
                                    state = 0;
                                    break;
                                case "endif":
                                    token.Type = TokenType.EndIf;
                                    state = 0;
                                    break;
                                default:
                                    token.Type = TokenType.Text;
                                    token.Text = "#" + token.Text + " ";
                                    state = 0;
                                    break;
                            }
                            result.Add(token);
                        }
                        else
                            curText.Append(c);
                        break;
                    case 2:
                        if (c == '/')
                        {
                            AppendIfNotEmpty();
                            state = 3;
                        }
                        else if (c == '*')
                        {
                            AppendIfNotEmpty();
                            state = 4;
                        }
                        else
                        {
                            curText.Append('/');
                            curText.Append(c);
                            state = 0;
                        }
                        break;
                    case 3:
                        if (c == '\n' || c == '\r')
                        {
                            result.Add(new Token
                            {
                                Type = TokenType.Comment,
                                Text = curText.ToString()
                            });
                            state = 0;
                            curText.Length = 0;
                        }
                        else
                            curText.Append(c);
                        break;

                    case 4:
                        if (c == '*')
                            state = 5;
                        else
                            curText.Append(c);
                        break;
                    case 5:
                        if (c == '/')
                        {
                            result.Add(new Token
                            {
                                Type = TokenType.Comment,
                                Text = curText.ToString()
                            });
                            curText.Length = 0;
                            state = 0;
                        }
                        else
                        {
                            curText.Append('*').Append(c);
                            state = 4;
                        }

                        break;
                    case 8:
                        if (c == '\n' || c == '\r')
                        {
                            if (curText.Length > 0)
                            {
                                result.Add(new Token
                                {
                                    Type = TokenType.Expression,
                                    Text = curText.ToString()
                                });
                                curText.Length = 0;
                            }
                            state = 0;
                        }
                        else
                            curText.Append(c);
                        break;
                }
                i++;
            }

            AppendIfNotEmpty();

            return result.ToArray();
        }

        public static string ParseShader(string data)
        {
            StringBuilder result = new StringBuilder();
            Token[] tokens = Tokenize(data);

            Dictionary<string, string> defs = [];
            Stack<bool> outWrite = [];

            int i = 0;
            int state = 0;

            outWrite.Push(true);
            bool curOutWrite = true;

            bool EvaluateExpression(string text)
            {
                string[] parts = text.Split("&&");
                foreach (string? part in parts.Select(a => a.Trim()))
                {
                    if (part.StartsWith("defined("))
                    {
                        string symbol = part[8..^1];
                        if (!defs.ContainsKey(symbol))
                            return false;
                    }
                    else if (part.StartsWith("!defined("))
                    {
                        string symbol = part[9..^1];
                        if (defs.ContainsKey(symbol))
                            return false;
                    }
                    else
                    {
                        string[] op = part.Split("==");
                        if (op.Length == 2)
                        {
                            string symbol = op[0].Trim();
                            if (defs.TryGetValue(symbol, out string? value))
                            {
                                if (value != op[1].Trim())
                                    return false;
                            }
                        }

                        else
                        {
                            //throw new NotSupportedException();
                        }

                    }
                }

                return true;
            }


            while (i < tokens.Length)
            {
                Token t = tokens[i];

                switch (state)
                {
                    case 0:
                        switch (t.Type)
                        {
                            case TokenType.Define:
                                state = 1;
                                break;
                            case TokenType.Undef:
                                state = 4;
                                break;
                            case TokenType.If:
                                state = 3;
                                break;
                            case TokenType.ElseIf:
                                outWrite.Pop();
                                state = 3;
                                break;
                            case TokenType.IfDef:
                                state = 2;
                                break;
                            case TokenType.IfnDef:
                                state = 21;
                                break;
                            case TokenType.Else:
                                bool lastWrite = outWrite.Pop();
                                outWrite.Push(!lastWrite);
                                curOutWrite = outWrite.All(a => a);
                                state = 0;
                                break;
                            case TokenType.EndIf:
                                outWrite.Pop();
                                curOutWrite = outWrite.All(a => a);
                                break;
                            case TokenType.Text:
                                if (curOutWrite)
                                    result.Append(t.Text);
                                break;
                            case TokenType.Comment:
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        break;
                    case 1:
                        if (t.Type == TokenType.Expression)
                        {
                            string[] parts = t.Text!.Split(' ');
                            defs[parts[0]] = parts.Length > 1 ? parts[1] : "";

                            if (parts.Length > 1)
                                result.Append("#define ").Append(t.Text).Append('\n');

                            state = 0;
                        }
                        else if (t.Type == TokenType.Comment)
                            continue;
                        else
                            state = 0;
                        break;
                    case 4:
                        if (t.Type == TokenType.Expression)
                        {
                            string[] parts = t.Text!.Split(' ');
                            defs.Remove(parts[0]);
                            state = 0;
                        }
                        else if (t.Type == TokenType.Comment)
                            continue;
                        else
                            state = 0;
                        break;
                    case 2:
                        if (t.Type == TokenType.Expression)
                        {
                            bool isDef = defs.ContainsKey(t.Text!);
                            outWrite.Push(isDef);
                            curOutWrite = outWrite.All(a => a);
                            state = 0;
                        }
                        else if (t.Type == TokenType.Comment)
                            continue;
                        else
                            state = 0;

                        break;
                    case 21:
                        if (t.Type == TokenType.Expression)
                        {
                            bool isnDef = !defs.ContainsKey(t.Text!);
                            outWrite.Push(isnDef);
                            curOutWrite = outWrite.All(a => a);
                            state = 0;
                        }
                        else if (t.Type == TokenType.Comment)
                            continue;
                        else
                            state = 0;

                        break;
                    case 3:
                        if (t.Type == TokenType.Expression)
                        {
                            bool isTrue = EvaluateExpression(t.Text!);
                            outWrite.Push(isTrue);
                            curOutWrite = outWrite.All(a => a);
                            state = 0;
                        }
                        else if (t.Type == TokenType.Comment)
                            continue;
                        else
                            state = 0;

                        break;

                }
                i++;

            }

            Regex regex = new(@"^\s*(?:\r?\n|\r)(?:\s*(?:\r?\n|\r))*", RegexOptions.Multiline);
            string text = regex.Replace(result.ToString(), Environment.NewLine);

            return text;
        }
    }
}
