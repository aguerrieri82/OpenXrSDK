using System.Diagnostics;
using System.Text;

namespace XrEngine
{
    public class ObjWriter
    {
        protected readonly StringBuilder _builder;

        public ObjWriter()
        {
            _builder = new StringBuilder();
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        }

        public void Add(TriangleMesh mesh)
        {
            Debug.Assert(mesh.Geometry != null);

            foreach (var v in mesh.Geometry.Vertices)
                _builder.AppendFormat("v {0} {1} {2}\n", v.Pos.X, v.Pos.Y, v.Pos.Z);

            _builder.AppendLine();

            foreach (var v in mesh.Geometry.Vertices)
                _builder.AppendFormat("vn {0} {1} {2}\n", v.Normal.X, v.Normal.Y, v.Normal.Z);

            _builder.AppendLine();

            var curI = 0;
            while (curI < mesh.Geometry.Indices.Length)
            {
                _builder.AppendFormat("f {0} {1} {2}\n",
                    mesh.Geometry.Indices[curI + 0] + 1,
                    mesh.Geometry.Indices[curI + 1] + 1,
                    mesh.Geometry.Indices[curI + 2] + 1);
                curI += 3;
            }

            _builder.AppendLine();
        }

        public string Text()
        {
            return _builder.ToString();
        }
    }
}
