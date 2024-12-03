namespace XrEngine
{
    public enum PlayerState
    {
        Stop,
        Play,
        Pause,
    }

    public interface IPlayer
    {
        void SetPlayState(PlayerState state);

        PlayerState PlayerState { get; }

        int Frame { get; set; }

        int Length { get; } 
    }
}
