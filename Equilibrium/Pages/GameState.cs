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

        if (ts.ShouldCheckForWin)
        {
            ts.ShouldCheckForWin = false;
            if (!IsWin && WinTime is null) WinTime = timeStamp + TimerMs;
        }

        await batch.LineWidthAsync(1 / GameScale);

#pragma warning disable CS0618
        await batch.ResetTransformAsync();
#pragma warning restore CS0618

        await batch.ClearRectAsync(0, 0, width, height);
        await batch.FillStyleAsync("AliceBlue");
        await batch.FillRectAsync(0, 0, width, height);

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


        foreach (var drag in ts.Drags)
        {
            WinTime = null;

            var body = Bodies[drag.BodyIndex];

            static float GetRotationDifference(float r1, float r2)
            {
                var diff = r1 - r2;
                while (diff > Math.PI) diff -= (float) Math.Tau;
                while (diff < -Math.PI) diff += (float)Math.Tau;
                return diff;
            }

            var rotation = (GetRotationDifference(drag.Next.Rotation, body.Body.Rotation) * dt) - body.Body.AngularVelocity;
            const float maxRotationAcc = 10;
            var adjustedRotation = Math.Clamp(rotation, -OneRotation * maxRotationAcc, OneRotation * maxRotationAcc);

             //TODO make it slow down as it approaches the target
            var vector = drag.Next.Position - body.Body.Position;
            var accVector = (vector * dt) - body.Body.LinearVelocity;
            
            const float maxAcc = 1;
            
            if (accVector.LengthSquared() > maxAcc)
            {
                accVector.Normalize();
                accVector  *= maxAcc;
            }
            
            body.Body.LinearVelocity += accVector;
            body.Body.AngularVelocity += adjustedRotation;
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
                        //Console.WriteLine($"Contact between {body.Body.Tag} and {contact.Other.Tag}");
                        break;
                    }

                    contact = contact.Next;
                }
            }

            //This will not draw walls
            await batch.DrawBodyAsync(body, "90");
        }

        Vector2? projectionPosition = null;

        //Draw Touch Drag Crosshairs
        foreach (var touchDrag in ts.Drags.Where(x=>x.DragIdentifier is TouchDragIdentifier))
        {
            var body = Bodies[touchDrag.BodyIndex];

            if (body.Shape is not null)
            {
                await batch.StrokeStyleAsync(body.Shape.Color);
                await batch.BeginPathAsync();

                await batch.MoveToAsync(body.Body.Position.X,0);
                await batch.LineToAsync(body.Body.Position.X, EquilibriumComponent.CanvasHeight );

                await batch.MoveToAsync(0, body.Body.Position.Y);
                await batch.LineToAsync(EquilibriumComponent.CanvasWidth, body.Body.Position.Y);

                await batch.StrokeAsync();


                if (projectionPosition is null)
                    projectionPosition = body.Body.Position;
            }
        }

        //await batch.StrokeStyleAsync("transparent");

        if (projectionPosition is not null)
        {
            const float projectionSize = 3;


            await batch.SetTransformAsync(
                ScaleConstants.XScale * projectionSize,
                0,
                0,
                ScaleConstants.YScale * projectionSize,
                ScaleConstants.XOffset - (projectionPosition.Value.X * ScaleConstants.XScale * (projectionSize-1 )),
                ScaleConstants.YOffset  - (projectionPosition.Value.Y * ScaleConstants.YScale* (projectionSize-1 ))
            );


            foreach (var shapeBody in Bodies)
            {
                if (shapeBody.Shape is not null)
                {
                    await batch.StrokeStyleAsync(shapeBody.Shape.Color + "40");
                    await batch.DrawBodyAsync(shapeBody, "40");
                }
                
            }
        }

        
#pragma warning disable CS0618
        await batch.ResetTransformAsync();
#pragma warning restore CS0618

        //Draw Win Progress Bar
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
                var x = random.NextSingle() * EquilibriumComponent.CanvasWidth / 2;
                var y = random.NextSingle() * EquilibriumComponent.CanvasHeight / 2;
                var newBody = shape.Create(World,
                    ScaleConstants.ScaleVector(x + EquilibriumComponent.CanvasWidth / 4 , y + EquilibriumComponent.CanvasHeight / 4),
                    0, Scale, BodyType.Dynamic);

                newBody.LinearVelocity = ScaleConstants.ScaleVector(new Vector2(0, -10 *random.NextSingle() * ShapeScale));

                newBody.Tag = "Dynamic " + shape.Name;
                //newBody.IsBullet = true;

                Bodies.Add(new ShapeBody(shape, newBody, shape.GetDrawable(Scale), ShapeBodyType.Dynamic));
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

    public void DragFirstBody(TransientState transientState) //Method for testing touch
    {
        var index = Bodies.FindIndex(x => x.Type == ShapeBodyType.Dynamic);
        var body = Bodies[index];
        body.Body.AngularVelocity = 0;
        var drag = new Drag(new TouchDragIdentifier(12345), body, index, Vector2.Zero);
        transientState.Drags.Add(drag);
    }

    public void MaybeStartDrag(DragIdentifier identifier, float x, float y, TransientState transientState)
    {
        var worldVector = ScaleConstants.ScaleVector(x, y);
        var fixture = World.TestPoint(worldVector);

        if (fixture is not null)
        {
            var bodyIndex = Bodies.FindIndex(shapeBody => shapeBody.Body.FixtureList.Contains(fixture));

            if(bodyIndex < 0)return;
        
            var body = Bodies[bodyIndex];

            if (body.Type == ShapeBodyType.Dynamic)
            {
                //body.Body.IsBullet = false;
                body.Body.AngularVelocity = 0;
                var drag = new Drag(identifier, body, bodyIndex, body.Body.Position - worldVector);
                transientState.Drags.Add(drag);
            }

        }
        else if(identifier is TouchDragIdentifier dfi)
        {
            var dragToRotate = transientState.Drags
                .FirstOrDefault(d => d.DragIdentifier is TouchDragIdentifier && d.Rotation is null);

            if(dragToRotate != null)
                dragToRotate.Rotation =
                new DragRotation(dfi, dragToRotate.Next.Position, worldVector, dragToRotate.Next.Rotation);
        }

        
    }

    
    public void EndDrag(DragIdentifier identifier, TransientState transientState)
    {
        var drag = transientState.Drags.FirstOrDefault(d => d.DragIdentifier == identifier);
        if (drag is null)
        {
            if (identifier is TouchDragIdentifier tdi)
            {
                var rotDrag = transientState.Drags.FirstOrDefault(d => d.Rotation?.RotationIdentifier == tdi);
                if (rotDrag != null)
                    rotDrag.Rotation = null;
            }


            return;
        }

        var body = Bodies[drag.BodyIndex];

        //Do this to reset contacts
        World.Remove(body.Body);
        World.Add(body.Body);

        transientState.Drags.Remove(drag);
        transientState.ShouldCheckForWin = true;
    }

    public void EndAllTouchDrags(TransientState transientState)
    {
        var tds = transientState.Drags.Where(x => x.DragIdentifier is TouchDragIdentifier).ToList();

        foreach (var drag in tds)
        {
            EndDrag(drag.DragIdentifier, transientState);
        }
    }

    public void OnDragMove(DragIdentifier identifier, float x, float y, TransientState transientState)
    {
        var drag = transientState.Drags.FirstOrDefault(d => d.DragIdentifier == identifier);
        var v = ScaleConstants.ScaleVector(x, y);

        if (drag is null)
        {
            if (identifier is TouchDragIdentifier tdi) //Touch rotation
            {
                var rotDrag = transientState.Drags.FirstOrDefault(d => d.Rotation?.RotationIdentifier == tdi);
                if (rotDrag != null)
                {
                    var rotation = rotDrag.Rotation!;
                    
                    var angle  = Math.Atan2(v.Y - rotation.CentrePosition.Y, v.X - rotation.CentrePosition.X) -
                                 Math.Atan2(rotation.StartPosition.Y - rotation.CentrePosition.Y, rotation.StartPosition.X - rotation.CentrePosition.X);

                    var fullAngle = rotation.StartRotation + angle;

                    //Console.WriteLine($"FullAngle: {fullAngle.ToString("F2")} Rotation: {angle.ToString("F2")}");

                    rotDrag.SetNext(rotDrag.Next.Position, (float)fullAngle);

                }
            }
            
            return;
        }

        drag.SetNext(v + drag.WorldCanvasOffset, drag.Next.Rotation);
    }

    const float OneRotation = (float) Math.Tau /  16;

    public void RotateDragged(int rotations, TransientState transientState)
    {
        foreach (var drag in transientState.Drags)
        {
            var currentRotations = Math.Round(drag.Next.Rotation / OneRotation);
            var newAngle = (currentRotations + rotations) * OneRotation;

            drag.SetNext(drag.Next.Position, (float)newAngle);
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