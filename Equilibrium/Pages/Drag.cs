namespace Equilibrium.Pages;

public class Drag
{
    public Drag(ShapeBody body, int bodyIndex, Vector2 worldCanvasOffset)
    {
        BodyIndex = bodyIndex;
        
        Next = (body.Body.Position, body.Body.Rotation);
        WorldCanvasOffset = worldCanvasOffset;
    }

    public int BodyIndex { get; }

    public Vector2 WorldCanvasOffset { get;  }


    public (Vector2 Position, float Rotation) Next { get; private set; } 

    public void SetNext(Vector2 position, float rotation)
    {
        Next = (position, rotation);
    }
}