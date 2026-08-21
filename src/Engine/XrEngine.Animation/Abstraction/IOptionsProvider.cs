namespace XrEngine.Animation
{
    public interface IOptionsProvider<TOptions>
    {
        TOptions Options { get; set; }
    }
}
