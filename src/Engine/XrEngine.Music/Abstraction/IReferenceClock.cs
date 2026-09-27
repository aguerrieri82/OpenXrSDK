using System.Diagnostics.CodeAnalysis;


namespace XrEngine.Music
{
    public interface IReferenceClock
    {
        ulong Now { get; }
    }

    public static class ReferenceClock
    {
        [AllowNull]
        public static IReferenceClock Default { get; set; }
    }
}
