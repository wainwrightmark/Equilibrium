namespace Equilibrium.Pages;

public static class Colors
{
    public const string Grey = "#a5a5a5";
    public const string Black = "#111111";
    public const string Green = "green";

    public static readonly string[] ShapeColors = new[]
    {
        "#3b8183", "#7ab317", "#7b3b3b", "#ff6b6b", "#f0a830", "#3299bb", "#b3cc57", "#d3ce3d", "#dfba69", "#7ccce5",
        "#53777a"
    };
}


public static class GameShapeHelper
{
    public static IReadOnlyList<GameShape> AllGameShapes { get; }

        = new List<GameShape>()
        {
            TriangleGameShape.Instance,
            //HemisphereGameShape.Instance,
            BoxGameShape.Instance,
            CircleGameShape.Instance,
            EllGameShape.Instance,
            //LollipopGameShape.Instance,
            
            CrossGameShape.Instance
        };

    private static readonly IReadOnlyDictionary<string, GameShape> GameShapeNameDictionary
        = AllGameShapes.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

    public static GameShape GetShapeByName(string name) => GameShapeNameDictionary[name];
}

public abstract record DrawableGameShape
{
    public abstract Task DrawAsync(Batch2D context, Transform transform);
}

public sealed record DrawableCircle(Vector2 Position, float Radius) : DrawableGameShape
{
    /// <inheritdoc />
    public override async Task DrawAsync(Batch2D context, Transform transform)
    {
        await context.BeginPathAsync();
        var translated = transform.Transform(Position);
        await context.ArcAsync(translated.X, translated.Y, Radius, 0, Math.Tau);


        await context.FillAsync(FillRule.EvenOdd);
        await context.StrokeAsync();
    }
}

//public sealed record DrawableEdge(Vector2 Vertex1, Vector2 Vertex2) : DrawableGameShape
//{
//    /// <inheritdoc />
//    public override async Task DrawAsync(Batch2D context, Transform transform)
//    {
//        await context.BeginPathAsync();

//        var translated1 = transform.Transform(Vertex1);
//        var translated2 = transform.Transform(Vertex2);

//        await context.MoveToAsync(translated1.X, translated1.Y);
//        await context.LineToAsync(translated2.X, translated2.Y);


//        await context.StrokeAsync();
//    }
//}

public sealed record DrawablePolygon(IReadOnlyList<Vector2> Vertices) : DrawableGameShape
{
    /// <inheritdoc />
    public override async Task DrawAsync(Batch2D context, Transform transform)
    {
        await context.BeginPathAsync();

        var firstVertex = transform.Transform(Vertices.First());
        await context.MoveToAsync(firstVertex.X, firstVertex.Y);

        foreach (var translated in Vertices.Skip(1).Select(v=> transform.Transform(v)))
        {
            await context.LineToAsync(translated.X, translated.Y);
        }

        await context.LineToAsync(firstVertex.X, firstVertex.Y);

        await context.FillAsync(FillRule.NonZero);
        await context.StrokeAsync();
    }
}



public abstract class GameShape
{
    public abstract int? RotationFraction { get; }


    public abstract int MaxRotations { get; }

    public float? RotationInterval =>
        (float)Math.Tau / RotationFraction;

    public float GetRotation(int numberOfRotations)
    {
        if (RotationInterval is null) return 0;
        return RotationInterval.Value * numberOfRotations;
    }

    public const float Density = 1;

    public abstract Body Create(World world, Vector2 position, float rotation, float scale, BodyType bodyType);

    public abstract DrawableGameShape GetDrawable(float scale);

    public abstract string Name { get; }

    public abstract string Color { get; }

    

    public abstract Vector2 GetLowestPosition(float shapeScale, int rotations);
}

public class CircleGameShape : GameShape
{
    private CircleGameShape() { }

    public static CircleGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override string Name => "Circle";

    /// <inheritdoc />
    public override int? RotationFraction => null;

    /// <inheritdoc />
    public override int MaxRotations => 0;

    /// <inheritdoc />
    public override Body Create(World world, Vector2 position, float rotation, float scale, BodyType bodyType)
    {
        return world.CreateCircle(scale / 2,
            Density,
            position,
            bodyType);
    }

    /// <inheritdoc />
    public override DrawableGameShape GetDrawable(float scale)
    {
        return new DrawableCircle(Vector2.Zero, scale / 2);
    }
    

    /// <inheritdoc />
    public override string Color => Colors.ShapeColors[0];

    /// <inheritdoc />
    public override Vector2 GetLowestPosition(float shapeScale, int rotations)
    {
        return new Vector2(0, -(shapeScale / 2));
    }
}

//public class HemisphereGameShape : ComplexPolygonGameShape
//{
//    private HemisphereGameShape()
//    {
//    }

//    public static HemisphereGameShape Instance { get; } = new();


//    /// <inheritdoc />
//    public override string Name => "Hemisphere";

//    /// <inheritdoc />
//    public override int? RotationFraction => 4;

//    /// <inheritdoc />
//    public override int MaxRotations => 4;

//    /// <inheritdoc />
//    protected override IEnumerable<Vertices> GetVertices(float scale)
//    {
//        yield return PolygonTools.CreateArc((float)Math.PI, 16, scale / 2);
//    }

//    /// <inheritdoc />
//    public override string Color => Colors.ShapeColors[1];

//}

public class BoxGameShape : ComplexPolygonGameShape
{
    private BoxGameShape()
    {
    }

    public static BoxGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override string Name => "Box";

    /// <inheritdoc />
    public override int? RotationFraction => 8;

    /// <inheritdoc />
    public override int MaxRotations => 2;

    /// <inheritdoc />
    protected override IEnumerable<Vertices> GetVertices(float scale)
    {
        yield return PolygonTools.CreateRectangle(scale / 4f, scale / 4f);
    }

    /// <inheritdoc />
    public override DrawableGameShape GetDrawable(float scale)
    {
        return new DrawablePolygon(
            GetVertices(scale).Single());
    }

    /// <inheritdoc />
    public override string Color => Colors.ShapeColors[2];
}

public abstract class ComplexPolygonGameShape : GameShape
{
    /// <inheritdoc />
    public override Body Create(World world, Vector2 position, float rotation, float scale, BodyType bodyType)
    {
        return world.CreateCompoundPolygon(GetVertices(scale).ToList(), Density, position, rotation, bodyType);
    }

    ///// <inheritdoc />
    //public override IEnumerable<Shape> GetShapes(float scale)
    //{
    //    foreach (var vertices in GetVertices(scale))
    //    {
    //        yield return new PolygonShape(vertices, 1);
    //    }
    //}

    protected abstract IEnumerable<Vertices> GetVertices(float scale);

    /// <inheritdoc />
    public override Vector2 GetLowestPosition(float shapeScale, int rotations)
    {
        var transform = new Transform(Vector2.Zero, GetRotation(rotations));
        var result= GetVertices(shapeScale)
            .SelectMany(x=>x)
            .Select(x=> transform.Transform(x)).MinBy(x=>x.Y);
        return result;
    }
    
}

public class EllGameShape : ComplexPolygonGameShape
{
    private EllGameShape()
    {
    }

    public static EllGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override string Name => "Ell";

    /// <inheritdoc />
    public override int? RotationFraction => 4;

    /// <inheritdoc />
    public override int MaxRotations => 4;


    protected override IEnumerable<Vertices> GetVertices(float scale)
    {
        var qScale = scale / 16;

        yield return PolygonTools.CreateRectangle(2 * qScale, qScale* 6);
        yield return PolygonTools.CreateRectangle(qScale * 2, 2 * qScale,
            new Vector2(4 * qScale, 4 * qScale), 0);
    }

    /// <inheritdoc />
    public override DrawableGameShape GetDrawable(float scale)
    {
        var qScale = scale / 8;
        return new DrawablePolygon(
            new Vector2[]
                {
                    new (-qScale,-3 * qScale ),
                    new (qScale,-3 * qScale),
                    new ( qScale, qScale),
                    new ( 3 *qScale, qScale),
                    new ( 3* qScale, 3 *qScale),
                    new ( - qScale, 3 *qScale),
                });
    }
    

    /// <inheritdoc />
    public override string Color => Colors.ShapeColors[3];
}

public class LollipopGameShape : ComplexPolygonGameShape
{
    private LollipopGameShape()
    {
    }

    public static LollipopGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override int? RotationFraction => 4;

    /// <inheritdoc />
    public override int MaxRotations => 4;

    /// <inheritdoc />
    public override string Name => "Lollipop";

    /// <inheritdoc />
    protected override IEnumerable<Vertices> GetVertices(float scale)
    {
        yield return PolygonTools.CreateCircle(scale / 4, 16);
        yield return PolygonTools.CreateRectangle(scale / 8, scale / 2, new Vector2(0, scale / 2), 0);
    }

    /// <inheritdoc />
    public override DrawableGameShape GetDrawable(float scale)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override string Color => Colors.ShapeColors[4];
}

public class TriangleGameShape : ComplexPolygonGameShape
{
    private TriangleGameShape()
    {
    }

    public static TriangleGameShape Instance { get; } = new();

    /// <inheritdoc />
    public override int? RotationFraction => 8;

    /// <inheritdoc />
    public override int MaxRotations => 8;

    /// <inheritdoc />
    public override string Name => "Triangle";


    /// <inheritdoc />
    protected override IEnumerable<Vertices> GetVertices(float scale)
    {
        yield return new Vertices(new[]
        {
            new Vector2(scale / 4, scale / 2),
            new Vector2(-scale / 4, 0),
            new Vector2(scale / 4, -scale / 2),
        });
    }

    /// <inheritdoc />
    public override DrawableGameShape GetDrawable(float scale)
    {
        return new DrawablePolygon(
            GetVertices(scale).Single());
    }

    /// <inheritdoc />
    public override string Color => Colors.ShapeColors[5];
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
    public override int? RotationFraction => 8;

    /// <inheritdoc />
    public override int MaxRotations => 2;


    protected override IEnumerable<Vertices> GetVertices(float scale)
    {
        yield return PolygonTools.CreateRectangle(scale / 2F, scale / 8f);
        yield return PolygonTools.CreateRectangle(scale / 8F, scale / 2f);
    }

    /// <inheritdoc />
    public override DrawableGameShape GetDrawable(float scale)
    {
        var qScale = scale / 8;
        return new DrawablePolygon(
                new Vector2[]
                {
                    new (-qScale, -4*qScale),
                    new (qScale, -4*qScale),    
                    new (qScale, -qScale),    
                    new (4*qScale, -qScale),    
                    new (4*qScale, qScale),    
                    new (qScale, qScale),    
                    new (qScale, 4* qScale),    
                    new (-qScale, 4* qScale),    
                    new (-qScale,  qScale),    
                    new (-4 *qScale,  qScale),    
                    new (-4 *qScale,  -qScale),    
                    new (-qScale,  -qScale),    
                });
    }

    /// <inheritdoc />
    public override string Color => Colors.ShapeColors[6];
}