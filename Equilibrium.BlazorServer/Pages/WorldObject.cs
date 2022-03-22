using System.Numerics;
using Box2D.NetStandard.Collision.Shapes;
using Box2D.NetStandard.Common;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Fixtures;
using Box2D.NetStandard.Dynamics.World;
using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;

namespace Equilibrium.BlazorServer.Pages;

public class WorldObject
{
    protected WorldObject(Body body)
    {
        Body = body;
    }

    public async Task Draw(Batch2D batch)
    {
        await batch.StrokeStyleAsync(StrokeColor);
        await batch.FillStyleAsync(FillColor);
        await batch.DrawBodyAsync(Body);
    }

    public Body Body { get; }

    public string StrokeColor { get; set; } = "black";
    public string FillColor { get; set; } = "grey";
}


public static class WorldObjectHelpers
{
    public static Body CreateRectangle(this World world, BodyType bodyType, Vector2 topLeft, float width, float height)
    {
        var bd = new BodyDef
        {
            type = bodyType,
            position = topLeft,
            angle = 0,
            linearVelocity = new Vector2(0, 0),
            angularVelocity = 0,
            linearDamping = 0,
            angularDamping = 0,
            allowSleep = true,
            awake = true,
            fixedRotation = false,
            bullet = false,
            // bd.active = true;
            gravityScale = 1
        };
        var body = world.CreateBody(bd);

        var fd = new FixtureDef
        {
            friction = 0.3f,
            restitution = 0.8f,
            density = 1f,
            isSensor = false,
            filter =
            {
                categoryBits = 1,
                maskBits = 65535,
                groupIndex = 0
            }
        };
        var shape = new PolygonShape(width, height);

        fd.shape = shape;

        body.CreateFixture(fd);
        return body;
    }


    public static Body CreateCircle(this World world, Vector2 position, float radius)
    {
        //world.

        var bd = new BodyDef
        {
            type = BodyType.Dynamic,
            position = position,
            angle = 0,
            linearVelocity = new Vector2(0, 0),
            angularVelocity = 0,
            linearDamping = 0,
            angularDamping = 0,
            allowSleep = true,
            awake = true,
            fixedRotation = false,
            bullet = true,
            // bd.active = true;
            gravityScale = 1
        };
        var body = world.CreateBody(bd);

        var fd = new FixtureDef
        {
            friction = 0.3f,
            restitution = 0.8f,
            density = 1f,
            isSensor = false,
            filter =
            {
                categoryBits = 1,
                maskBits = 65535,
                groupIndex = 0
            }
        };
        var shape = new CircleShape()
        {
            Center = new Vector2(0, 0),
            Radius = radius
        };

        fd.shape = shape;

        body.CreateFixture(fd);
        return body;
    }


    public static async Task DrawBodyAsync(this Batch2D context, Body body)
    {
        var current = body.GetFixtureList();

        while (current is not null)
        {
            await DrawFixtureAsync(context, current, body.Transform);

            current = current.Next;
        }
    }

    public static Task DrawFixtureAsync(this Batch2D context, Fixture fixture, Transform transform)
    {
        return fixture.Shape switch
        {
            ChainShape shape => DrawShapeAsync(context, shape, transform),
            CircleShape shape => DrawShapeAsync(context, shape, transform),
            EdgeShape shape => DrawShapeAsync(context, shape, transform),
            PolygonShape shape => DrawShapeAsync(context, shape, transform),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static async Task DrawShapeAsync(this Batch2D context, ChainShape shape, Transform transform
    )
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

    public static async Task DrawShapeAsync(this Batch2D context, CircleShape shape,
        Transform transform
    )
    {
        await context.BeginPathAsync();
        var translated = transform.Transform(shape.Center);
        await context.ArcAsync(translated.X, translated.Y, shape.Radius, 0, Math.Tau);

        await context.StrokeAsync();
        await context.FillAsync(FillRule.EvenOdd);
    }

    public static async Task DrawShapeAsync(this Batch2D context, EdgeShape shape, Transform transform
    )
    {
        await context.BeginPathAsync();

        var translated1 = transform.Transform(shape.Vertex1);
        var translated2 = transform.Transform(shape.Vertex2);

        await context.MoveToAsync(translated1.X, translated1.Y);
        await context.LineToAsync(translated2.X, translated2.Y);


        await context.StrokeAsync();
    }

    public static async Task DrawShapeAsync(this Batch2D context, PolygonShape shape, Transform transform
    )
    {
        await context.BeginPathAsync();
        bool first = true;

        foreach (var shapeVertex in shape.GetVertices())
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
        await context.FillAsync(FillRule.EvenOdd);
    }


    public static Vector2 Transform(this Transform transform, Vector2 vector2)
    {
        return transform.p + transform.q.Solve(vector2);
    }
}