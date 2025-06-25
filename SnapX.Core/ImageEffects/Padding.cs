namespace SnapX.Core.ImageEffects;

public struct Padding
{
    public int Left, Top, Right, Bottom;

    public Padding(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public override string? ToString() => $"{Left}, {Top}, {Right}, {Bottom}";
}
