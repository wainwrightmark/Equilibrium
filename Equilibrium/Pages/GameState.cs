namespace Equilibrium.Pages;

public class GameState
{
    public GameState(Level level)
    {
        World = new World(new Vector2(0, Gravity));
        Level = level;
    }


    public World World { get; }
    public Level Level { get; private set; }
    public List<ShapeBody> Bodies { get; } = new();


    public ScaleConstants ScaleConstants { get; } = new(GameScale, GameScale, 0, 0);

    public bool IsWin { get; private set; } = false;

    public float? WinTime { get; private set; }
    public const float TimerMs = 5000;
    public const float Gravity = 10;//0 / GameScale;

    /// <summary>
    /// How many physics units to one canvas pixel
    /// </summary>
    public const float GameScale = 100;

    /// <summary>
    /// The relative size of shapes
    /// </summary>
    public const float ShapeScale = 60;

    public const float Scale = ShapeScale / GameScale;

    public event Action<GameState>? StateChanged;


    public async Task StepAndDraw(float timeStamp,
        Batch2D batch,
        int width,
        int height,
        TransientState ts)
    {
        if (timeStamp >= WinTime)
        {
            Victory();
        }

        if (ts.DragJustEnded)
        {
            ts.DragJustEnded = false;
            if (!IsWin && WinTime is null) WinTime = timeStamp + TimerMs;
        }

        await batch.LineWidthAsync(1 / GameScale);

#pragma warning disable CS0618
        await batch.ResetTransformAsync();
#pragma warning restore CS0618

        await batch.ClearRectAsync(0, 0, width, height);


        await batch.StrokeStyleAsync(Colors.Black);

        await batch.SetTransformAsync(
            ScaleConstants.XScale,
            0,
            0,
            ScaleConstants.YScale,
            ScaleConstants.XOffset,
            ScaleConstants.YOffset
        );


        var dt = ts.Step(timeStamp);

        if (ts.Drag is not null)
        {
            WinTime = null;

            var body = Bodies[ts.Drag.BodyIndex];

            var rotation = ts.Drag.Next.Rotation - body.Body.Rotation;
            var vector = ts.Drag.Next.Position - body.Body.Position;
            var adjustedVector = vector * dt;
            var adjustedRotation = Math.Clamp(rotation * dt, -OneRotation * 10, OneRotation * 10);
            const float maxVel = 5;

            if (adjustedVector.LengthSquared() > maxVel)
            {
                adjustedVector.Normalize();
                adjustedVector *= maxVel;
            }

            body.Body.LinearVelocity = adjustedVector;
            body.Body.AngularVelocity = adjustedRotation;
        }

        try
        {
            World.Step(dt / 1000);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return;
        }

        var minPosition = ScaleConstants.ScaleVector(Vector2.Zero);
        var maxPosition =
            ScaleConstants.ScaleVector(EquilibriumComponent.CanvasWidth, EquilibriumComponent.CanvasHeight);

        foreach (var body in Bodies)
        {
            //Stop win timer if an object hits the wall
            if (IsWin == false && WinTime is not null && body.Type == ShapeBodyType.Wall)
            {
                var contact = body.Body.ContactList;
                while (contact is not null)
                {
                    if (contact.Other.BodyType == BodyType.Dynamic)
                    {
                        WinTime = null;
                        Console.WriteLine($"Contact between {body.Body.Tag} and {contact.Other.Tag}");
                        break;
                    }

                    contact = contact.Next;
                }
            }

            //Make sure no body can leave the world
            if (body.Body.Position.X < minPosition.X) body.Body.Position = body.Body.Position with { X = minPosition.X };
            if (body.Body.Position.Y < minPosition.Y) body.Body.Position = body.Body.Position with { Y = minPosition.Y };
            
            if (body.Body.Position.X > maxPosition.X) body.Body.Position = body.Body.Position with { X = maxPosition.X };
            if (body.Body.Position.Y > maxPosition.Y) body.Body.Position = body.Body.Position with { Y = maxPosition.Y };
            

            if (body.Shape is not null)
            {
                await batch.DrawBodyAsync(body);
            }
        }

#pragma warning disable CS0618
        await batch.ResetTransformAsync();
#pragma warning restore CS0618


        if (WinTime is not null)
        {
            await batch.FillStyleAsync("grey");
            await batch.FillRectAsync(50, 50, 110, 50);

            await batch.FillStyleAsync("green");
            await batch.FillRectAsync(55, 55, 100 * ((WinTime.Value - timeStamp) / TimerMs), 40);
        }
    }

    public void ChangeLevel(Level newLevel, TransientState transientState, Random random)
    {
        Level = newLevel;
        Restart(transientState, random);
    }

    public void Restart(TransientState transientState, Random random)
    {
        IsWin = false;
        WinTime = null;
        World.Clear();
        Bodies.Clear();
        Bodies.AddRange(Level.SetupWorld(World,
            EquilibriumComponent.CanvasWidth / GameScale,
            EquilibriumComponent.CanvasHeight / GameScale,
            Scale));

        foreach (var shapeMetadata in Level.Shapes)
        {
            var shape = GameShapeHelper.GetShapeByName(shapeMetadata.Shape);
            for (var i = 0; i < shapeMetadata.Number; i++)
            {
                var x = random.NextSingle() * EquilibriumComponent.CanvasWidth;
                var y = random.NextSingle() * EquilibriumComponent.CanvasHeight;
                var newBody = shape.Create(World,
                    ScaleConstants.ScaleVector(x, y),
                    0, Scale, BodyType.Dynamic);

                newBody.Tag = "Dynamic " + shape.Name;
                //newBody.IsBullet = true;

                Bodies.Add(new ShapeBody(shape, newBody, ShapeBodyType.Dynamic));
            }
        }

        transientState.RestartGame();
        StateChanged?.Invoke(this);
    }


    public void Victory()
    {
        if (IsWin is false)
        {
            WinTime = null;
            IsWin = true;
            StateChanged?.Invoke(this);
        }
    }

    

    public void MaybeStartDrag(float x, float y, TransientState transientState)
    {
        var worldVector = ScaleConstants.ScaleVector(x, y);
        var fixture = World.TestPoint(worldVector);

        if (fixture is null) return;

        var bodyIndex = Bodies.FindIndex(shapeBody => shapeBody.Body.FixtureList.Contains(fixture));

        if(bodyIndex < 0)return;
        
        var body = Bodies[bodyIndex];

        if (body.Type == ShapeBodyType.Dynamic)
        {
            //body.Body.IsBullet = false;
            body.Body.AngularVelocity = 0;
            transientState.Drag = new Drag(body, bodyIndex, body.Body.Position - worldVector);
        }
    }

    
    public void EndDrag(TransientState transientState)
    {
        if (transientState.Drag is not null)
        {
            var body = Bodies[transientState.Drag.BodyIndex];

            //Do this to reset contacts
            World.Remove(body.Body);
            World.Add(body.Body);


            transientState.Drag = null;
            transientState.DragJustEnded = true;
        }
    }

    public void OnDragMove(float x, float y, TransientState transientState)
    {
        if (transientState.Drag is not null)
        {
            var v = ScaleConstants.ScaleVector(x, y);
            transientState.Drag.SetNext(v + transientState.Drag.WorldCanvasOffset, transientState.Drag.Next.Rotation);
        }
    }

    const float OneRotation = (float) Math.Tau /  16;

    public void RotateDragged(int rotations, TransientState transientState)
    {
        if (transientState.Drag is not null)
        {
            var currentRotations = Math.Round(transientState.Drag.Next.Rotation / OneRotation);
            var newAngle = (currentRotations + rotations) * OneRotation;

            transientState.Drag.SetNext(transientState.Drag.Next.Position, (float)newAngle);
        }
    }

    public string? Message
    {
        get
        {
            return IsWin switch
            {
                true => "You are Victorious!",
                false => null
            };
        }
    }
}