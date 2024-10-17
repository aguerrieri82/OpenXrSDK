using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.OpenXr
{
    public class SceneModelInfo
    {
        public IList<string>? Labels { get; set; }
        
        public SceneModelType Type { get; set; }    

        public Pose3 Pose { get; set; }

        public Guid AnchorId { get; set; }

        public Space Space { get; set; }    

        public Vector2 Size { get; set; }

        public Geometry3D? Geometry { get; set; }   
    }

    public enum SceneModelType
    {
        Unknown,
        Wall,
        Floor,
        Ceiling,
        Window,
        Door,
        Mesh
    }


    public interface ISceneModelFactory
    {
        Object3D? CreateModel(SceneModelInfo model);
    }
}
