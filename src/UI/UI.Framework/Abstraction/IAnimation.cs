namespace CanvasUI
{
    public interface IAnimation
    {
        bool IsStarted { get; set; }

        TimeSpan StartTime { get; set; }    

        TimeSpan Duration { get; set; }

        void Step(float t);
    }
}
