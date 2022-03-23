using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Common.Decomposition;
using tainicom.Aether.Physics2D.Dynamics;

namespace Equilibrium.Pages;

public abstract class GameShape
{
    public abstract float? RotationInterval { get; }
    
    public const float Density = 1;

    public abstract Body Create(World world, Vector2 position, float rotation, float scale);

    public abstract IEnumerable<Shape> GetShapes(float scale);

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
    public override Body Create(World world, Vector2 position, float rotation, float shapeScale)
    {
        return world.CreateCircle(shapeScale / 2 ,
            Density, 
            position, 
            BodyType.Dynamic);
    }

    /// <inheritdoc />
    public override IEnumerable<Shape> GetShapes(float scale)
    {
        yield return new CircleShape(scale / 2, Density);
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
    public override Body Create(World world, Vector2 position, float rotation, float scale)
    {
        return world .CreateRectangle(scale, scale, 1, position, rotation, BodyType.Dynamic);
    }

    /// <inheritdoc />
    public override IEnumerable<Shape> GetShapes(float scale)
    {
        yield return new PolygonShape(PolygonTools.CreateRectangle(scale / 2f, scale / 2f), Density);
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
    public override Body Create(World world, Vector2 position, float rotation, float scale)
    {
        return world.CreateGear(
            scale / 3,
            4,50, scale / 3, Density, position,rotation, BodyType.Dynamic);
    }

    /// <inheritdoc />
    public override IEnumerable<Shape> GetShapes(float scale)
    {
        var gear = PolygonTools.CreateGear(scale / 3, 4, 50, scale / 3);
        foreach (var vertices in Triangulate.ConvexPartition(gear, TriangulationAlgorithm.Earclip))
        {
            yield return new PolygonShape(vertices, 1);
        }
    }
}