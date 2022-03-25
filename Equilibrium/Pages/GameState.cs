namespace Equilibrium.Pages;

public class TransientState
{
    public (float X, float Y)? MousePosition { get; set; } = null;

    public float LastTimestamp { get; set; } = 0;

    public int FramesPerSecond { get; set; } = 0;

    public ChosenShape? ChosenShape { get; set; } = null;

    public float Step(float newTimestamp)
    {
        var dt = newTimestamp - LastTimestamp;
        var newFps = 1000 / (newTimestamp - LastTimestamp);
        FramesPerSecond = (int)newFps;
        LastTimestamp = newTimestamp;
        return dt;
    }

    public void RestartGame()
    {
        MousePosition = null;
        ChosenShape = null;
    }
}

public readonly record struct ScaleConstants(float XScale, float YScale, float XOffset, float YOffset)
{
    public Vector2 ScaleVector(float x, float y)
    {
        var vec = new Vector2(
            (x - XOffset) / XScale,
            (y - YOffset) / YScale);
        return vec;
    }
}

public class GameState
{
    public GameState(Level level)
    {
        World = new World(new Vector2(0, Gravity));
        Level = level;
    }


    public World World { get; }
    public Level Level { get; private set; }
    public List<ShapeBodyPair> Bodies { get; } = new();
    public Stack<(ChosenShape Shape, Vector2 Position)> ShapesToAdd { get; } = new();

    public List<GameShape> AddedShapes { get; } = new();

    public ScaleConstants ScaleConstants { get; } = new(GameScale, GameScale, 0, 0);

    public bool? IsWin { get; private set; } = null;

    public float? WinTime { get; private set; }
    public const float TimerMs = 5000;
    public const float Gravity = 100 / GameScale;

    /// <summary>
    /// How many physics units to one canvas pixel
    /// </summary>
    public const float GameScale = 10;

    /// <summary>
    /// The relative size of shapes
    /// </summary>
    public const float ShapeScale = 60;

    public const float Scale = ShapeScale / GameScale;

    public event Action<GameState> StateChanged;

    public void ChangeLevel(Level newLevel, TransientState transientState)
    {
        Level = newLevel;
        Restart(transientState);
    }

    public void Restart(TransientState transientState)
    {
        IsWin = null;
        WinTime = null;
        World.Clear();
        Bodies.Clear();
        AddedShapes.Clear();
        Bodies.AddRange(Level.SetupWorld(World,
            EQC.CanvasWidth / GameScale,
            EQC.CanvasHeight / GameScale,
            Scale));

        transientState.RestartGame();
        StateChanged?.Invoke(this);
    }


    public IEnumerable<(GameShape shape, int count)> RemainingShapes()
    {
        return Level.GetShapes().Concat(AddedShapes.Select(x => (x, -1)))
            .GroupBy(x => x.Item1, x => x.Item2)
            .Select(group => (group.Key, group.Sum()));
    }

    public void GameOver()
    {
        if (IsWin != true)
        {
            IsWin = false;
            WinTime = null;
            StateChanged?.Invoke(this);
        }
    }

    public void Victory()
    {
        if (IsWin is null)
        {
            WinTime = null;
            IsWin = true;
            StateChanged?.Invoke(this);
        }
    }

    public async Task StepAndDraw(float timeStamp, Batch2D batch, int width, int height, TransientState transientState)
    {
        if (timeStamp >= WinTime)
        {
            Victory();
        }

        await batch.LineWidthAsync(1 / GameScale);

        await batch.ResetTransformAsync();

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

        var dt = transientState.Step(timeStamp);

        try
        {
            World.Step(dt / 1000);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return;
        }
        

        var shapeAdded = false;
        while (ShapesToAdd.TryPop(out var shapeToAdd))
        {
            var newBody = shapeToAdd.Shape.Shape.Create(World, 
                shapeToAdd.Position, 
                shapeToAdd.Shape.Rotation, Scale, BodyType.Dynamic);
            Bodies.Add(new ShapeBodyPair(shapeToAdd.Shape.Shape, newBody, ShapeBodyType.Dynamic));
            shapeAdded = true;
        }

        if (shapeAdded && IsWin is null)
        {
            if (RemainingShapes().All(x => x.count <= 0))
            {
                WinTime = timeStamp + TimerMs;
            }
        }


        foreach (var body in Bodies)
        {
            if (IsWin is null && body.Type == ShapeBodyType.Wall)
            {
                var contact = body.Body.ContactList;
                while (contact is not null)
                {
                    if (contact.Other.BodyType == BodyType.Dynamic)
                    {
                        GameOver();
                    }

                    contact = contact.Next;
                }
            }

            if (body.Shape is not null)
            {
                await batch.DrawBodyAsync(body);
            }

            
        }

        await batch.ResetTransformAsync();
        

        var chosenShape = transientState.ChosenShape;

        if (transientState.MousePosition is not null && chosenShape.HasValue)
        {
            await batch.FillStyleAsync(chosenShape.Value.Shape.Color);
            await batch.LineWidthAsync(3);

            var (x, y) = transientState.MousePosition.Value;

            foreach (var shape in chosenShape.Value.Shape.GetShapes(ShapeScale))
            {
                await batch.DrawShapeAsync(
                    shape,
                    new Transform(new Vector2(x, y), chosenShape.Value.Rotation));
            }
        }

        if (WinTime is not null)
        {
            await batch.FillStyleAsync("grey");
            await batch.FillRectAsync(50, 50, 110, 50);

            await batch.FillStyleAsync("green");
            await batch.FillRectAsync(55, 55, 100 * ((WinTime.Value - timeStamp) / TimerMs), 40);
        }
    }

    public string? Message
    {
        get
        {
            switch (IsWin)
            {
                case true:
                    return "You are Victorious!";
                case false:
                    return "Sorry! You lost!";
                default:
                {
                    return null;
                }
            }
        }
    }


    public void MaybeAddChosenShape(float x, float y, TransientState transientState)
    {
        var chosenShape = transientState.ChosenShape;

        if (chosenShape.HasValue)
        {
            var vec = ScaleConstants.ScaleVector(x, y);

            ShapesToAdd.Push((chosenShape.Value, vec));
            AddedShapes.Add(chosenShape.Value.Shape);

            transientState.ChosenShape = null;
        }

        transientState.MousePosition = null;
    }
}