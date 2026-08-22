using System.Text;

namespace XrEngine
{
    public static class ShaderUpdateBuilderExtensions
    {
        extension(ShaderUpdateBuilder self)
        {
            public void SetIncludesSlot(string name, params string[] includes)
            {
                self.SetSlot(name, () =>
                {
                    var sb = new StringBuilder();

                    foreach (var include in includes)
                        sb.Append("#include \"").Append(include).AppendLine("\"");

                    return sb.ToString();
                });
            }

            public void SetVsIncludes(params string[] includes)
            {
                self.SetIncludesSlot(ShaderSlots.VertexIncludes, includes);
            }

            public void SetFsIncludes(params string[] includes)
            {
                self.SetIncludesSlot(ShaderSlots.FragmentIncludes, includes);
            }

            public void SetFragmentLoader(string code)
            {
                self.SetSlot(ShaderSlots.FragmentLoader, code);
            }

            public void SetVertexLocalTransform(string code)
            {
                self.SetSlot(ShaderSlots.VertexLocalTransforms, code);
            }

            public void SetSlot(string name, string code)
            {
                self.SetSlot(name, () => code);
            }
        }
    }
}
