using System;
using System.Collections.Generic;
using System.Text;

namespace XrEditor
{
    public sealed class StartupArgs
    {
        public static StartupArgs Parse()
        {
            return Parse(Environment.GetCommandLineArgs());
        }

        public static StartupArgs Parse(string[] args)
        {
            var result = new StartupArgs();

            for (var i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-assembly":
                        result.Assemblies.Add(ReadValue(args, ref i, "-assembly"));
                        break;

                    case "-entry":
                        result.Entry = ReadValue(args, ref i, "-entry");
                        break;

                    case "-assets":
                        result.Assets.Add(ReadValue(args, ref i, "-assets"));
                        break;

                    default:
                        throw new ArgumentException($"Unknown argument '{args[i]}'");
                }
            }

            return result;
        }

        private static string ReadValue(string[] args, ref int index, string name)
        {
            if (++index >= args.Length)
                throw new ArgumentException($"Missing value for '{name}'");

            return args[index];
        }

        public List<string> Assemblies { get; } = [];

        public string? Entry { get; private set; }

        public List<string> Assets { get; } = [];

    }
}
