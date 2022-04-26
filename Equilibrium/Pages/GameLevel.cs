

namespace Equilibrium.Pages;

public sealed record ShapeMetadata(string Shape, int Number);

public sealed record Level(
    //string InitialShape,
    //int InitialShapeRotations,
    IReadOnlyList<ShapeMetadata> Shapes)
{
    public static readonly Level Basic =

        new (//BoxGameShape.Instance.Name, 0,
            GameShapeHelper.AllGameShapes.Select(x => new ShapeMetadata(Shape: x.Name,Number: 1))
                .ToList()
        );

    public static Level MakeRandomLevel(Random random)
    {
        var totalShapes = random.Next(4, 9);

        var shapes = Enumerable.Range(0, totalShapes)
            .Select(_ => RandomShape(random))
            .GroupBy(x => x.Name)
            .Select(x => new ShapeMetadata(x.Key, x.Count()))
            .ToList();

        return new Level(shapes);

    }

    private static GameShape RandomShape(Random random)
    {
        return GameShapeHelper.AllGameShapes[random.Next(GameShapeHelper.AllGameShapes.Count)];
    }       

    public IEnumerable<(GameShape Shape, int Count)> GetShapes()
    {
        return Shapes.Select(valueTuple => (GameShapeHelper.GetShapeByName(valueTuple.Shape), valueTuple.Number));
    }

    public IEnumerable<ShapeBody> SetupWorld(World world, float width, float height, float shapeScale)
    {
        //Walls

        var bottomWall =world.CreateEdge(new Vector2(width, height), new Vector2(0, height));
        var topWall =  world.CreateEdge(new Vector2(0, 0), new Vector2(width, 0));
        var leftWall = world.CreateEdge(new Vector2(0, height), new Vector2(0, 0));
        var rightWall = world.CreateEdge(new Vector2(width, 0), new Vector2(width, height));

        bottomWall.Tag = "Bottom Wall";
        topWall.Tag = "Top Wall";
        leftWall.Tag = "Left Wall";
        rightWall.Tag = "Right Wall";

        yield return new (null, bottomWall , null,ShapeBodyType.Wall) ;
        yield return new (null, rightWall,null, ShapeBodyType.Wall) ;
        yield return new (null, topWall, null,ShapeBodyType.Wall) ;
        yield return new (null, leftWall,null, ShapeBodyType.Wall) ;

        //var initialShape = GameShapeHelper.GetShapeByName(InitialShape);

        //var lowestPosition = initialShape.GetLowestPosition(shapeScale, InitialShapeRotations);

        //var initialShapePosition = new Vector2(width / 2, height + lowestPosition.Y - shapeScale);
        //var initialRotation = initialShape.GetRotation(InitialShapeRotations);


        //var body = initialShape.Create(world, initialShapePosition, initialRotation, shapeScale, BodyType.Static);
        //body.Tag = "Static " + initialShape.Name;
        //yield return new ShapeBody(initialShape, body, initialShape.GetDrawable(shapeScale), ShapeBodyType.Static);
    }
    


}