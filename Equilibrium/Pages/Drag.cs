namespace Equilibrium.Pages;
using static Constants;

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
        
        Desired = (body.Body.Position, body.Body.Rotation);
        WorldCanvasOffset = worldCanvasOffset;
    }

    public DragIdentifier DragIdentifier { get; }
    public int BodyIndex { get; }

    public Vector2 WorldCanvasOffset { get;  }


    public (Vector2 Position, float Rotation) Desired { get; private set; } 

    public void SetNext(Vector2 position, float rotation)
    {
        Desired = (position, rotation);
    }

    public DragRotation? Rotation { get; set; }

    public void ApplyToBody(Body body, float dt)
    {
        var currentRotDistance = (GetRotationDifference(Desired.Rotation, body.Rotation) / dt); //rad/s
        var projectedRotDistance = currentRotDistance - body.AngularVelocity; //rad/s
        const float maxRotationAcc = 1 * OneRotation; //rad
        var adjustedRotation = Math.Clamp(projectedRotDistance, -maxRotationAcc / dt, maxRotationAcc /dt);

        body.AngularVelocity += adjustedRotation;

        //How far awy the body is
        var currentDistance = Desired.Position - (body.Position) ; //m
        //How far the body will travel
        var projectedDistance = currentDistance - (body.LinearVelocity * dt); //m

        //This is the acceleration required to get to the target position
        var accVector = (projectedDistance / (dt * dt)); //m/s squared
            
        const float maxAcc = 10f;//m/s squared

        if (accVector.LengthSquared() > maxAcc)
        {
            //The required acceleration is bigger than the max. So we reduce it
            if (currentDistance.LengthSquared() > projectedDistance.LengthSquared())
            {
                accVector.Normalize(); //unit
                accVector  *= maxAcc; //m / s2
            }
            //Do not limit acceleration as we are breaking
        }
            
        body.LinearVelocity += (accVector * dt); //m/s
        
    }
    

    static float GetRotationDifference(float r1, float r2)
    {
        var diff = r1 - r2;
        while (diff > Math.PI) diff -= (float) Math.Tau;
        while (diff < -Math.PI) diff += (float)Math.Tau;
        return diff;
    }
}

public record DragRotation(TouchDragIdentifier RotationIdentifier, 
    Vector2 CentrePosition,
    Vector2 StartPosition,
    float StartRotation)
{
}