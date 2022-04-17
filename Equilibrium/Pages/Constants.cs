namespace Equilibrium.Pages;

public static class Constants
{
    /// <summary>
    /// One rotation of a shape
    /// </summary>
    public const float OneRotation = (float) Math.Tau /  16;

    /// <summary>
    /// How long you must wait for a win to be confirmed
    /// </summary>
    public const float TimerMs = 3000;

    /// <summary>
    /// Game Gravity
    /// </summary>
    public const float Gravity = 10;

    /// <summary>
    /// How many physics units to one canvas pixel
    /// </summary>
    public const float GameScale = 100;

    /// <summary>
    /// The relative size of shapes
    /// </summary>
    public const float ShapeScale = 60;

    /// <summary>
    /// General scale
    /// </summary>
    public const float Scale = ShapeScale / GameScale;

    /// <summary>
    /// The width of the game world in pixels
    /// </summary>
    public const float GameWidth = 350;

    /// <summary>
    /// The height of the game world in pixels
    /// </summary>
    public const float GameHeight = 800;
}
