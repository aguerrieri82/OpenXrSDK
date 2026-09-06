namespace XrEngine.Components
{
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
