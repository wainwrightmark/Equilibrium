﻿using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics;

namespace Equilibrium.Pages;

public abstract class GameLevel
{
    

    public abstract IEnumerable<(GameShape Shape, int Count)> GetShapes();

    public abstract IEnumerable<Body> SetupWorld(World world, float width, float height, float shapeScale);


    protected static IEnumerable<Body> BuildWalls(World world, float width, float height, float shapeScale)
    {
        yield return SetAsWall(world.CreateEdge(new Vector2(0, 0), new Vector2(width, 0))) ;
        yield return SetAsWall(world.CreateEdge(new Vector2(width, 0), new Vector2(width, height)));
        yield return SetAsWall(world.CreateEdge(new Vector2(width, height), new Vector2(0, height)));
        yield return SetAsWall(world.CreateEdge(new Vector2(0, height), new Vector2(0, 0)));
    }

    private static Body SetAsWall(Body b)
    {
        b.Tag = "wall";
        return b;
    }
}


public class LevelOne : GameLevel
{
    /// <inheritdoc />
    public override IEnumerable<Body> SetupWorld(World world, float width, float height, float shapeScale)
    {
        foreach (var wall in BuildWalls(world, width, height, shapeScale))
        {
            yield return wall;
        }

        yield return world.CreateRectangle(shapeScale / 2, shapeScale * 3, 
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