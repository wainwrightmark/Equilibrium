using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics;

namespace Equilibrium.Pages;

public abstract class GameLevel
{
    public abstract IEnumerable<Body> SetupWorld(World world, float width, float height, float scale);

    public abstract IEnumerable<(GameShape Shape, int Count)> GetShapes();
}


public class LevelOne : GameLevel
{
    /// <inheritdoc />
    public override IEnumerable<Body> SetupWorld(World world, float width, float height, float scale)
    {
        yield return world.CreateRectangle(scale / 2, scale * 3, 
            1, 
            new Vector2(width / 2,height));
    }

    /// <inheritdoc />
    public override IEnumerable<(GameShape Shape, int Count)> GetShapes()
    {
        yield return (BoxGameShape.Instance, 2);
        yield return (CrossGameShape.Instance, 2);
        yield return (CircleGameShape.Instance, 1);
    }
}


public class ChosenShape
{
    public ChosenShape(GameShape shape, int numberOfRotations)
    {
        Shape = shape;
        NumberOfRotations = numberOfRotations;
    }

    public GameShape Shape { get; init; }
    public int NumberOfRotations { get; init; }

    public float Rotation => Shape.RotationInterval is null ? 0 : NumberOfRotations * Shape.RotationInterval.Value;
}