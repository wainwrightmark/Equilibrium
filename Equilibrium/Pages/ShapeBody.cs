namespace Equilibrium.Pages;


public enum ShapeBodyType
{
    Wall,
    Static,
    Dynamic
}

public readonly record struct ShapeBody(GameShape? Shape,
    Body Body,
    DrawableGameShape? DrawableGameShape,
    ShapeBodyType Type);