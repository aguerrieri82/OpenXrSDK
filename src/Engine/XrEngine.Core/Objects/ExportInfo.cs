using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Objects
{
    public enum MeshExportFormat
    {
        Obj
    }

    public class MeshExportInfo<T> where T : Object3D
    {
        readonly Object3D _owner;

        public MeshExportInfo(Object3D owner)
        {
            _owner = owner;
            Path = "d:\\";
        }
        public void Export()
        {
            _owner.EnsureId();

            var name = FileName ?? _owner.Id.ToString();
            var path = Path ?? "";
            var fullPath = System.IO.Path.Combine(path, name);

            if (Format == MeshExportFormat.Obj)
            {
                if (_owner is not TriangleMesh mesh)
                    throw new NotSupportedException();

                fullPath += ".obj";

                var objWriter = new ObjWriter();
                objWriter.Add(mesh);

                File.WriteAllText(fullPath, objWriter.Text());
            }
            else
                throw new NotSupportedException();

            Log.Info(this, "Export completed");
        }

        public string? FileName { get; set; }

        public string? Path { get; set; }    

        public MeshExportFormat Format { get; set; }
    }
}
