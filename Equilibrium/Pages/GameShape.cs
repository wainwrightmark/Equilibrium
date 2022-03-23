using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Common.Decomposition;
using tainicom.Aether.Physics2D.Dynamics;

namespace Equilibrium.Pages;

public abstract class GameShape
{
    public abstract float? RotationInterval { get; }

    public const float Scale = 60;

    public abstract Body Create(World world, Vector2 position, float rotation);

    public abstract IEnumerable<Shape> GetShapes();

    public abstract string Name { get; }
}

public class CircleGameShape : GameShape
{
    private CircleGameShape() {}

    public static CircleGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override string Name => "Circle";

    /// <inheritdoc />
    public override float? RotationInterval => null;

    /// <inheritdoc />
    public override Body Create(World world, Vector2 position, float rotation)
    {
        return world.CreateCircle(Scale / 2, 1, position, BodyType.Dynamic);
    }

    /// <inheritdoc />
    public override IEnumerable<Shape> GetShapes()
    {
        yield return new CircleShape(Scale / 2, 1);
    }
}

public class BoxGameShape : GameShape
{
    private BoxGameShape() {}

    public static BoxGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override string Name => "Box";

    /// <inheritdoc />
    public override float? RotationInterval => (float) Math.Tau / 8;

    /// <inheritdoc />
    public override Body Create(World world, Vector2 position, float rotation)
    {
        return world .CreateRectangle(Scale, Scale, 1, position, rotation, BodyType.Dynamic);
    }

    /// <inheritdoc />
    public override IEnumerable<Shape> GetShapes()
    {
        yield return new PolygonShape(PolygonTools.CreateRectangle(Scale / 2f, Scale / 2f), 1);
    }
}

public class CrossGameShape : GameShape
{
    private CrossGameShape() {}

    public static CrossGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override string Name => "Cross";

    /// <inheritdoc />
    public override float? RotationInterval => (float) Math.Tau / 8;

    /// <inheritdoc />
    public override Body Create(World world, Vector2 position, float rotation)
    {
        return world.CreateGear(
            Scale / 3,
            4,50, Scale / 3, 1, position,rotation, BodyType.Dynamic);
    }

    /// <inheritdoc />
    public override IEnumerable<Shape> GetShapes()
    {
        var gear = PolygonTools.CreateGear(Scale / 3, 4, 50, Scale / 3);
        foreach (var vertices in Triangulate.ConvexPartition(gear, TriangulationAlgorithm.Earclip))
        {
            yield return new PolygonShape(vertices, 1);
        }
    }
}