namespace Equilibrium.Pages;

public static class DrawHelpers
{
    public static async Task DrawBodyAsync(this Batch2D context, ShapeBodyPair shapeBodyPair)
    {
        if(shapeBodyPair.Shape is null)return;
        
        string color;
        if (shapeBodyPair.Type == ShapeBodyType.Static)
            color = Colors.Grey;
        else color = shapeBodyPair.Shape.Color;

        await context.FillStyleAsync(color);

        var transform = shapeBodyPair.Body.GetTransform();

        foreach (var fixture in shapeBodyPair.Body.FixtureList ?? Enumerable.Empty<Fixture>())
        {
            await DrawFixtureAsync(context, fixture, transform);
        }
    }

    public static Task DrawFixtureAsync(this Batch2D context, Fixture fixture, Transform transform)
    {
        return DrawShapeAsync(context, fixture.Shape, transform);
    }

    public static Task DrawShapeAsync(this Batch2D context, Shape shape, Transform transform)
    {
        return shape switch
        {
            ChainShape chain => DrawChainAsync(context, chain, transform),
            CircleShape circle => DrawCircleAsync(context, circle, transform),
            EdgeShape edge => DrawEdgeAsync(context, edge, transform),
            PolygonShape polygon => DrawPolygonAsync(context, polygon, transform),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static async Task DrawChainAsync(this Batch2D context, ChainShape shape, Transform transform)
    {
        await context.BeginPathAsync();

        var first = true;

        foreach (var shapeVertex in shape.Vertices)
        {
            var translated = transform.Transform(shapeVertex);
            if (first)
            {
                await context.MoveToAsync(translated.X, translated.Y);
                first = false;
            }
            else
            {
                await context.LineToAsync(translated.X, translated.Y);
            }
        }

        await context.StrokeAsync();
    }

    public static async Task DrawCircleAsync(this Batch2D context, CircleShape gameShape,
        Transform transform)
    {
        await context.BeginPathAsync();
        var translated = transform.Transform(gameShape.Position);
        await context.ArcAsync(translated.X, translated.Y, gameShape.Radius, 0, Math.Tau);


        await context.FillAsync(FillRule.EvenOdd);
        await context.StrokeAsync();
    }

    public static async Task DrawEdgeAsync(this Batch2D context, EdgeShape shape, Transform transform)
    {
        await context.BeginPathAsync();

        var translated1 = transform.Transform(shape.Vertex1);
        var translated2 = transform.Transform(shape.Vertex2);

        await context.MoveToAsync(translated1.X, translated1.Y);
        await context.LineToAsync(translated2.X, translated2.Y);


        await context.StrokeAsync();
    }

    public static async Task DrawPolygonAsync(this Batch2D context, PolygonShape shape, Transform transform
    )
    {
        await context.BeginPathAsync();

        var firstVertex = transform.Transform(shape.Vertices.First());
        await context.MoveToAsync(firstVertex.X, firstVertex.Y);

        foreach (var translated in shape.Vertices.Skip(1).Select(v=> transform.Transform(v)))
        {
            await context.LineToAsync(translated.X, translated.Y);
        }

        await context.LineToAsync(firstVertex.X, firstVertex.Y);

        await context.FillAsync(FillRule.EvenOdd);
        await context.StrokeAsync();
    }


    public static Vector2 Transform(this Transform transform, Vector2 vector2)
    {
        var x = Complex.Multiply(vector2, ref transform.q);
        return transform.p + x; // + transform.q. .Solve(vector2);
    }
}