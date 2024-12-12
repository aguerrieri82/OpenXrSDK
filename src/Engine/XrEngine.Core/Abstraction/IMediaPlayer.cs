namespace XrEngine.Media
{
    public interface IMediaPlayer
    {
        float Duration { get; }
        
        float Position { get; set; }

        float Volume { get; set; }

        void Open(string path);
        
        void Pause();
        
        void Play();

        void Stop();
    }
}