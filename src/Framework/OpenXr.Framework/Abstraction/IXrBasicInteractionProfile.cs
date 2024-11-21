namespace OpenXr.Framework
{
    public interface IXrBasicInteractionProfile
    {
        XrInteractionProfileHand<XrInteractionProfileHandLeft>? Left { get; }

        XrInteractionProfileHand<XrInteractionProfileHandRight>? Right { get; }
    }
}
