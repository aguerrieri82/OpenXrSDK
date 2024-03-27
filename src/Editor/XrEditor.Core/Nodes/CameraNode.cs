using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;

namespace XrEditor.Nodes
{
    public class CameraNode<T> : EngineObjectNode<T> where T : Camera
    {
        public CameraNode(T value) : base(value)
        {
        }

        public override IconView? Icon => new()
        {
            Color = "#7B1FA2",
            Name = "icon_videocam"
        };
    }
}
