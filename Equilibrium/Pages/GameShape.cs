using tainicom.Aether.Physics2D.Collision.Shapes;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics;

namespace Equilibrium.Pages;

public abstract class GameShape
{
    public abstract float? RotationInterval { get; }

    public const float Density = 1;

    public abstract Body Create(World world, Vector2 position, float rotation, float scale);

    public abstract IEnumerable<Shape> GetShapes(float scale);

    public abstract string Name { get; }

    public static IEnumerable<GameShape> AllGameShapes()
    {
        yield return CircleGameShape.Instance;
        yield return HemisphereGameShape.Instance;
        yield return BoxGameShape.Instance;
        yield return EllGameShape.Instance;
        yield return LollipopGameShape.Instance;
        yield return TriangleGameShape.Instance;
        yield return CrossGameShape.Instance;
    }
}

public class CircleGameShape : GameShape
{
    private CircleGameShape()
    {
    }

    public static CircleGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override string Name => "Circle";

    /// <inheritdoc />
    public override float? RotationInterval => null;

    /// <inheritdoc />
    public override Body Create(World world, Vector2 position, float rotation, float shapeScale)
    {
        return world.CreateCircle(shapeScale / 2,
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

public class HemisphereGameShape : ComplexPolygonGameShape
{
    private HemisphereGameShape()
    {
    }

    public static HemisphereGameShape Instance { get; } = new();


    /// <inheritdoc />
    public override string Name => "Hemisphere";

    /// <inheritdoc />
    public override float? RotationInterval => (float) Math.Tau / 4f;


    /// <inheritdoc />
    protected override IEnumerable<Vertices> GetVertices(float scale)
    {
        yield return PolygonTools.CreateArc((float)Math.PI, 16, scale / 2);

        //yield return PolygonTools.CreateCapsule(scale / 2 + float.Epsilon, scale / 4 , 16, scale /4, 1);
    }
}

public class BoxGameShape : GameShape
{
    private BoxGameShape()
    {
    }

    public static BoxGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override string Name => "Box";

    /// <inheritdoc />
    public override float? RotationInterval => (float)Math.Tau / 8;

    /// <inheritdoc />
    public override Body Create(World world, Vector2 position, float rotation, float scale)
    {
        return world.CreateRectangle(scale, scale, 1, position, rotation, BodyType.Dynamic);
    }

    /// <inheritdoc />
    public override IEnumerable<Shape> GetShapes(float scale)
    {
        yield return new PolygonShape(PolygonTools.CreateRectangle(scale / 2f, scale / 2f), Density);
    }
}


public abstract class ComplexPolygonGameShape : GameShape
{
    /// <inheritdoc />
    public override Body Create(World world, Vector2 position, float rotation, float scale)
    {
        return world.CreateCompoundPolygon(GetVertices(scale).ToList(), Density, position, rotation, BodyType.Dynamic);
    }

    /// <inheritdoc />
    public override IEnumerable<Shape> GetShapes(float scale)
    {
        foreach (var vertices in GetVertices(scale).ToList())
        {
            yield return new PolygonShape(vertices, 1);
        }
    }

    protected abstract IEnumerable<Vertices> GetVertices(float scale);
}

public class EllGameShape : ComplexPolygonGameShape
{
    private EllGameShape() { }

    public static EllGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override string Name => "Ell";

    /// <inheritdoc />
    public override float? RotationInterval => (float)Math.Tau / 4;


    protected override IEnumerable<Vertices> GetVertices(float scale)
    {
        var qScale = scale / 8;

        yield return PolygonTools.CreateRectangle(4 * qScale, qScale);
        yield return PolygonTools.CreateRectangle(qScale, 6 * qScale,
            new Vector2(3f * qScale, 5f * qScale), 0);
    }
}

public class LollipopGameShape : ComplexPolygonGameShape
{
    private LollipopGameShape() { }

    public static LollipopGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override float? RotationInterval => (float) Math.Tau / 4;

    /// <inheritdoc />
    public override string Name => "Lollipop";

    /// <inheritdoc />
    protected override IEnumerable<Vertices> GetVertices(float scale)
    {
        yield return PolygonTools.CreateCircle(scale / 4, 16);
        yield return PolygonTools.CreateRectangle(scale / 8, scale / 2, new Vector2(0, scale / 2), 0);
    }
}

public class TriangleGameShape : ComplexPolygonGameShape
{
    private TriangleGameShape() { }

    public static TriangleGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override float? RotationInterval => (float) Math.Tau / 8;

    /// <inheritdoc />
    public override string Name => "Triangle";


    /// <inheritdoc />
    protected override IEnumerable<Vertices> GetVertices(float scale)
    {
        yield return new Vertices(new[]
        {
            new Vector2(scale / 4,  scale / 2),
            new Vector2(-scale /4, 0),
            new Vector2(scale / 4, -scale / 2),
        });
    }
}

public class CrossGameShape : ComplexPolygonGameShape
{
    private CrossGameShape()
    {
    }

    public static CrossGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override string Name => "Cross";

    /// <inheritdoc />
    public override float? RotationInterval => (float)Math.Tau / 8;


    protected override IEnumerable<Vertices> GetVertices(float scale)
    {
        yield return PolygonTools.CreateRectangle(scale / 2F, scale / 8f);
        yield return PolygonTools.CreateRectangle(scale / 8F,  scale / 2f);
    }
}