namespace Equilibrium.Pages;

public readonly record struct ScaleConstants(float XScale, float YScale, float XOffset, float YOffset)
{
    public Vector2 ScaleVector(float x, float y)
    {
        var vec = new Vector2(
            (x - XOffset) / XScale,
            (y - YOffset) / YScale);
        return vec;
    }public Vector2 ScaleVector(Vector2 v)
    {
        var vec = new Vector2(
            (v.X - XOffset) / XScale,
            (v.Y - YOffset) / YScale);
        return vec;
    }
}