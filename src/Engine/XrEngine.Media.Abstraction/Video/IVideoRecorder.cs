namespace XrEngine.Media
{

    public interface IVideoRecorder
    {
        NativeSurface StartRecording(string outPath, VideoRecordOptions options);

        bool ProcessEncodedFrames(out long timestamp);

        void StopRecording();

    }
}
