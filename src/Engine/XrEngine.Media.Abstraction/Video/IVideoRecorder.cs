namespace XrEngine.Media
{

    public interface IVideoRecorder
    {
        NativeSurface StartRecording(string outPath, VideoRecordOptions options);

        long ProcessEncodedFrames();

        void StopRecording();

    }
}
