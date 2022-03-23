namespace Equilibrium.Pages;

public readonly record struct ChosenShape(GameShape Shape, int NumberOfRotations)
{
    public float Rotation => Shape.RotationInterval is null ? 0 : NumberOfRotations * Shape.RotationInterval.Value;
}