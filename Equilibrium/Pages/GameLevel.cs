

namespace Equilibrium.Pages;


public sealed record Level(string InitialShape,
    int InitialShapeRotations,
    IReadOnlyList<(string Shape, int Number)> Shapes)
{
    public static readonly Level Basic =

        new (BoxGameShape.Instance.Name, 0,
            GameShapeHelper.AllGameShapes.Select(x => (Shape: x.Name,Number: 1))
                .ToList()
        );

    public static Level MakeRandomLevel(Random random)
    {
        var initialShape = RandomShape(random);
        var initialRotation =
            initialShape.RotationFraction is null ? 0 : random.Next(initialShape.RotationFraction.Value);
        var totalShapes = random.Next(4, 9);

        var shapes = Enumerable.Range(0, totalShapes)
            .Select(_ => RandomShape(random))
            .GroupBy(x => x.Name)
            .Select(x => (x.Key, x.Count()))
            .ToList();

        return new Level(initialShape.Name, initialRotation, shapes);

    }

    private static GameShape RandomShape(Random random)
    {
        return GameShapeHelper.AllGameShapes[random.Next(GameShapeHelper.AllGameShapes.Count)];
    }       

    public IEnumerable<(GameShape Shape, int Count)> GetShapes()
    {
        return Shapes.Select(valueTuple => (GameShapeHelper.GetShapeByName(valueTuple.Shape), valueTuple.Number));
    }

    public IEnumerable<ShapeBodyPair> SetupWorld(World world, float width, float height, float shapeScale)
    {
        //Walls
        yield return new (null, world.CreateEdge(new Vector2(0, 0), new Vector2(width, 0)), ShapeBodyType.Wall) ;
        yield return new (null, world.CreateEdge(new Vector2(width, 0), new Vector2(width, height)), ShapeBodyType.Wall) ;
        yield return new (null, world.CreateEdge(new Vector2(width, height), new Vector2(0, height)), ShapeBodyType.Wall) ;
        yield return new (null, world.CreateEdge(new Vector2(0, height), new Vector2(0, 0)), ShapeBodyType.Wall) ;

        var initialShape = GameShapeHelper.GetShapeByName(InitialShape);

        var lowestPosition = initialShape.GetLowestPosition(shapeScale, InitialShapeRotations);

        var initialShapePosition = new Vector2(width / 2, lowestPosition.Y + height);
        var initialRotation = initialShape.RotationInterval is null
            ? 0
            : initialShape.RotationInterval.Value * InitialShapeRotations;


        var body = initialShape.Create(world, initialShapePosition, initialRotation, shapeScale, BodyType.Static);

        yield return new ShapeBodyPair(initialShape, body, ShapeBodyType.Static);
    }
    


}