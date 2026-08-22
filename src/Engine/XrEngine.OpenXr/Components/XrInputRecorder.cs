using OpenXr.Framework;

namespace XrEngine.OpenXr
{
    public class XrInputRecorder : BaseFrameRecorder<XrInputRecorder.XrRecordFrame, Scene3D>
    {
        public class XrRecordFrame : RecordFrame
        {
            public long XrTime;

            public Dictionary<string, XrInputState>? Inputs;
        }

        public XrInputRecorder()
        {
            OutFile = "inputs.json";
        }

        protected override bool CreateFrame(XrRecordFrame frame)
        {
            if (XrApp.Current == null)
                return false;

            frame.XrTime = XrApp.Current.FramePredictedDisplayTime;
            frame.Inputs = [];

            foreach (var input in XrApp.Current.Inputs.Values)
                frame.Inputs[input.Name] = input.GetState();

            return true;
        }
    }
}
