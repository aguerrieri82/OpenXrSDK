using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrEngine.Components
{
    public class TransformRecorder : BaseFrameRecorder<TransformRecorder.TransformRecordFrame, Object3D>
    {

        public class TransformRecordFrame : RecordFrame
        {
            public Matrix4x4 WorldMatrix;
        }

        public TransformRecorder()
        {
            OutFile = "transform.json";
        }

        protected override bool CreateFrame(TransformRecordFrame frame)
        {
            if (RemoveDuplicated && _session!.Frames!.Count > 0 && 
                _session.Frames[^1].WorldMatrix == _host.WorldMatrix)
                return false;

            frame.WorldMatrix = _host.WorldMatrix;

            return true;
        }

        public bool RemoveDuplicated { get; set; }
    }

    public class TransformPlayer : BaseFramePlayer<TransformRecorder.TransformRecordFrame, Object3D>
    {
        public TransformPlayer()
        {
            SourceFile = "transform.json";
        }

        protected override void ApplyFrame(TransformRecorder.TransformRecordFrame frame)
        {
            _host.WorldMatrix = frame.WorldMatrix;  
        }
    }
}
