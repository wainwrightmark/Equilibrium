namespace Equilibrium.Pages;


public enum ShapeBodyType
{
    Wall,
    Static,
    Dynamic
}

public readonly record struct ShapeBodyPair(GameShape? Shape, Body Body, ShapeBodyType Type);