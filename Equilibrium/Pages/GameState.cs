namespace Equilibrium.Pages;
using static Constants;

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

    public ShapeBody? FixedBody { get; set; }
    public ShapeBody? PreviousFixedBody { get; set; }

    public ScaleConstants ScaleConstants { get; } = new(GameScale, GameScale, 0, 0);

    public bool IsWin { get; private set; }

    public float? WinTime { get; private set; }

    public int? WinProgressPercent { get; private set; }

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

        ts.YOffset = ScaleConstants.YOffset -
            (height < GameHeight ? GameHeight - height : 0);

        await batch.LineWidthAsync(1 / GameScale);

#pragma warning disable CS0618
        await batch.ResetTransformAsync();
#pragma warning restore CS0618

        await batch.ClearRectAsync(0, 0, width, height);
        await batch.FillStyleAsync("White");
        await batch.FillRectAsync(0, 0, width, height);

        await batch.StrokeStyleAsync(Colors.Black);

        await batch.SetTransformAsync(
            ScaleConstants.XScale,
            0,
            0,
            ScaleConstants.YScale,
            ScaleConstants.XOffset,
            ts.YOffset
        );


        var dt = ts.Step(timeStamp);
        var physicsDt = dt / 1000;

        foreach (var drag in ts.Drags)
        {
            WinTime = null;

            var body = Bodies[drag.BodyIndex];

            drag.ApplyToBody(body.Body, physicsDt);
        }

        try
        {
            World.Step(physicsDt);
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
            await batch.DrawBodyAsync(body, "90", body == FixedBody);
        }

        Vector2? projectionPosition = null;

        //Draw Touch Drag Crosshairs
        foreach (var touchDrag in ts.Drags.Where(x => x.DragIdentifier is TouchDragIdentifier))
        {
            var body = Bodies[touchDrag.BodyIndex];

            if (body.Shape is not null)
            {
                await batch.StrokeStyleAsync(body.Shape.Color);
                await batch.BeginPathAsync();

                await batch.MoveToAsync(body.Body.Position.X, 0);
                await batch.LineToAsync(body.Body.Position.X, GameHeight);

                await batch.MoveToAsync(0, body.Body.Position.Y);
                await batch.LineToAsync(GameWidth, body.Body.Position.Y);

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
                ScaleConstants.XOffset - (projectionPosition.Value.X * ScaleConstants.XScale * (projectionSize - 1)),
                ts.YOffset - (projectionPosition.Value.Y * ScaleConstants.YScale * (projectionSize - 1))
            );


            foreach (var shapeBody in Bodies)
            {
                if (shapeBody.Shape is not null)
                {
                    await batch.StrokeStyleAsync(shapeBody.Shape.Color + "40");
                    await batch.DrawBodyAsync(shapeBody, "40", shapeBody == FixedBody);
                }

            }
        }


#pragma warning disable CS0618
        await batch.ResetTransformAsync();
#pragma warning restore CS0618

        //Draw Win Progress Bar
        if (WinTime is not null)
        {
            WinProgressPercent = Convert.ToInt32(100 * ((WinTime.Value - timeStamp) / TimerMs));

            //await batch.FillStyleAsync("grey");
            //await batch.FillRectAsync(50, 50, 110, 50);

            //await batch.FillStyleAsync("green");
            //await batch.FillRectAsync(55, 55, 100 * ((WinTime.Value - timeStamp) / TimerMs), 40);
        }
        else
        {
            WinProgressPercent = null;
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
        FixedBody = null;
        PreviousFixedBody=null;
        World.Clear();
        Bodies.Clear();
        Bodies.AddRange(Level.SetupWorld(World,
            GameWidth / GameScale,
            GameHeight / GameScale,
            Scale));

        foreach (var shapeMetadata in Level.Shapes)
        {
            var shape = GameShapeHelper.GetShapeByName(shapeMetadata.Shape);
            for (var i = 0; i < shapeMetadata.Number; i++)
            {
                var x = random.NextSingle() * GameWidth / 2;
                var y = random.NextSingle() * GameHeight / 2;
                var newBody = shape.Create(World,
                    ScaleConstants.ScaleVector(x + GameWidth / 4, y + GameHeight / 4),
                    0, Scale, BodyType.Dynamic);

                newBody.LinearVelocity = ScaleConstants.ScaleVector(new Vector2(0, -10 * random.NextSingle() * ShapeScale));

                newBody.Tag = "Dynamic " + shape.Name;
                newBody.IsBullet = true;

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
        var worldVector = ScaleConstants.ScaleVector(x, y - transientState.YOffset);
        var selectedFixture = World.TestPoint(worldVector);

        
        if (selectedFixture is not null)
        {
            var bodyIndex = Bodies.FindIndex(shapeBody => shapeBody.Body.FixtureList.Contains(selectedFixture));

            if (bodyIndex < 0) return;

            var body = Bodies[bodyIndex];

            if(body == FixedBody) //If this is the fixed body, make it dynamic again
            {
                body.Body.BodyType = BodyType.Dynamic;
                FixedBody = null;
                PreviousFixedBody = body;
            }

            if (body.Type == ShapeBodyType.Dynamic)
            {
                //body.Body.IsBullet = false;
                body.Body.AngularVelocity = 0;
                body.Body.IgnoreGravity = true;
                var drag = new Drag(identifier, body, bodyIndex, body.Body.Position - worldVector);
                transientState.Drags.Add(drag);
            }

        }
        else if (identifier is TouchDragIdentifier dfi)
        {
            var dragToRotate = transientState.Drags
                .FirstOrDefault(d => d.DragIdentifier is TouchDragIdentifier && d.Rotation is null);

            if (dragToRotate != null)
                dragToRotate.Rotation =
                new DragRotation(dfi, dragToRotate.Desired.Position, worldVector, dragToRotate.Desired.Rotation);
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

        if (body == PreviousFixedBody)
        {
            //The user changed their mind about the fixed body - unfix it
            PreviousFixedBody = null;
        }
        else if (FixedBody is null)
        {
            //This was the first body to be moved - make it the fixed body
            FixedBody = body;
            body.Body.BodyType = BodyType.Static;
        }

        body.Body.IgnoreGravity = false;

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
        var v = ScaleConstants.ScaleVector(x, y - transientState.YOffset);

        if (drag is null)
        {
            if (identifier is TouchDragIdentifier tdi) //Touch rotation
            {
                var rotDrag = transientState.Drags.FirstOrDefault(d => d.Rotation?.RotationIdentifier == tdi);
                if (rotDrag != null)
                {
                    var rotation = rotDrag.Rotation!;

                    var angle = Math.Atan2(v.Y - rotation.CentrePosition.Y, v.X - rotation.CentrePosition.X) -
                                 Math.Atan2(rotation.StartPosition.Y - rotation.CentrePosition.Y, rotation.StartPosition.X - rotation.CentrePosition.X);

                    var fullAngle = rotation.StartRotation + angle;

                    //Console.WriteLine($"FullAngle: {fullAngle.ToString("F2")} Rotation: {angle.ToString("F2")}");

                    rotDrag.SetNext(rotDrag.Desired.Position, (float)fullAngle);

                }
            }

            return;
        }

        drag.SetNext(v + drag.WorldCanvasOffset, drag.Desired.Rotation);
    }



    public void RotateDragged(int rotations, TransientState transientState)
    {
        foreach (var drag in transientState.Drags)
        {
            var currentRotations = Math.Round(drag.Desired.Rotation / OneRotation);
            var newAngle = (currentRotations + rotations) * OneRotation;

            drag.SetNext(drag.Desired.Position, (float)newAngle);
        }

    }


}