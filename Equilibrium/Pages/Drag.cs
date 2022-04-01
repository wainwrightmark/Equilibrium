namespace Equilibrium.Pages;


public abstract record DragIdentifier;

public sealed record MouseDragIdentifier : DragIdentifier
{
    private MouseDragIdentifier() { }

    public static MouseDragIdentifier Instance { get; } = new();
}

public sealed record TouchDragIdentifier(long Id) : DragIdentifier;

public class Drag
{
    public Drag(DragIdentifier dragIdentifier, ShapeBody body, int bodyIndex, Vector2 worldCanvasOffset)
    {
        DragIdentifier = dragIdentifier;
        BodyIndex = bodyIndex;
        
        Next = (body.Body.Position, body.Body.Rotation);
        WorldCanvasOffset = worldCanvasOffset;
    }

    public DragIdentifier DragIdentifier { get; }
    public int BodyIndex { get; }

    public Vector2 WorldCanvasOffset { get;  }


    public (Vector2 Position, float Rotation) Next { get; private set; } 

    public void SetNext(Vector2 position, float rotation)
    {
        Next = (position, rotation);
    }

    public DragRotation? Rotation { get; set; }
}

public record DragRotation(TouchDragIdentifier RotationIdentifier, 
    Vector2 CentrePosition,
    Vector2 StartPosition,
    float StartRotation)
{
}