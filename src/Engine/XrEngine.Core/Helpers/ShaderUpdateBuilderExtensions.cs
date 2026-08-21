using System.Text;


namespace XrEngine
{
    public static class ShaderUpdateBuilderExtensions
    {
        public static void SetIncludesSlot(this ShaderUpdateBuilder self, string name, params string[] includes)
        {
            self.SetSlot(name, () =>
            {
                var sb = new StringBuilder();

                foreach (var include in includes)
                    sb.Append("#include \"").Append(include).AppendLine("\"");

                return sb.ToString();
            });
        }

        public static void SetVsIncludes(this ShaderUpdateBuilder self, params string[] includes)
        {
             self.SetIncludesSlot(ShaderSlots.VertexIncludes, includes);
        }

        public static void SetFsIncludes(this ShaderUpdateBuilder self, params string[] includes)
        {
            self.SetIncludesSlot(ShaderSlots.FragmentIncludes, includes);
        }

        public static void SetFragmentLoader(this ShaderUpdateBuilder self, string code)
        {
            self.SetSlot(ShaderSlots.FragmentLoader, code);
        }

        public static void SetVertexLocalTransform(this ShaderUpdateBuilder self, string code)
        {
            self.SetSlot(ShaderSlots.VertexLocalTransforms, code);
        }

        public static void SetSlot(this ShaderUpdateBuilder self, string name, string code)
        {
            self.SetSlot(name, () => code);
        }
    }
}
