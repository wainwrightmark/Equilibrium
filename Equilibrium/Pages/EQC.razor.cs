using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using Majorsoft.Blazor.Components.Common.JsInterop.ElementInfo;
using Majorsoft.Blazor.Components.Common.JsInterop.GlobalMouseEvents;
using Majorsoft.Blazor.Components.Common.JsInterop.Resize;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using tainicom.Aether.Physics2D.Common;
using tainicom.Aether.Physics2D.Dynamics;
using DotNetObjectReference = Microsoft.JSInterop.DotNetObjectReference;


namespace Equilibrium.Pages;

public partial class EQC
{
    private ElementReference container;

    private Canvas helper_canvas;
    private Context2D _canvasContext;

    private DomRect containerRect;

    public World World { get; private set; }

    public List<Body> Bodies { get; private set; } = new();

    public GameLevel Level { get; private set; } = new LevelOne();

    private const float CanvasWidth = 400;
    private const float CanvasHeight = 400;

    [Inject] public IJSRuntime JsRuntime { get; set; }

    [Inject] public IResizeHandler ResizeHandler { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _canvasContext = await helper_canvas.GetContext2DAsync();
            containerRect = await container.GetClientRectAsync()!;
            await ResizeHandler.RegisterPageResizeAsync(OnResize);

            World = new World(new Vector2(0, 100));
            Clear();


            await JsRuntime.InvokeAsync<object>("initGame",
                new[] { DotNetObjectReference.Create(this) as object });
        }
    }

    protected async Task OnResize(ResizeEventArgs args)
    {
        containerRect = await container.GetClientRectAsync()!;
    }

    // needed to calculate fps
    float _lastTimestamp = 0;
    float _fps = 0;

    private float XScale = 1;
    private float YScale = 1;
    private float XOffset = 0;
    private float YOffset = 0;


    public void Clear()
    {
        World.Clear();
        Bodies.Clear();
        AddedShapes.Clear();
        Bodies.AddRange(Level.SetupWorld(World, CanvasWidth, CanvasHeight, 60));
    }

    /// <summary>
    /// This method will be called 60 times per second by the requestanimationframe from javascript.
    /// </summary>
    /// <param name="timeStamp">The current timestamp</param>
    /// <param name="width">The width of the canvas</param>
    /// <param name="height">The height of the canvas</param>
    /// <returns>A completed task</returns>
    [JSInvokable]
    public async ValueTask GameLoop(float timeStamp, int width, int height)
    {
        await using var batch = _canvasContext.CreateBatch();

        await batch.LineWidthAsync(1);

        await batch.ResetTransformAsync();

        await batch.ClearRectAsync(0, 0, width, height);


        await batch.FillStyleAsync("blue");
        await batch.StrokeStyleAsync("black");

        await batch.SetTransformAsync(
            XScale,
            0,
            0,
            YScale,
            XOffset,
            YOffset
        );

        var dt = timeStamp - _lastTimestamp;
        var newFps = 1000 / (timeStamp - _lastTimestamp);
        if (Math.Abs(newFps - _fps) >= 2)
            _fps = newFps;
        _lastTimestamp = timeStamp;

        World.Step(dt / 1000);

        while (_bodiesToAdd.TryPop(out var bodyToAdd))
        {
            var newBody = bodyToAdd.Shape.Shape.Create(World, bodyToAdd.Position, bodyToAdd.Shape.Rotation);
            Bodies.Add(newBody);
        }

        foreach (var body in Bodies)
        {
            await batch.DrawBodyAsync(body);
        }

        await batch.FillStyleAsync("yellow");

        if (mouseX is not null && mouseY is not null && chosenShape is not null)
        {
            foreach (var shape in chosenShape.Shape.GetShapes())
            {
                await batch.DrawShapeAsync(
                    shape,
                    new Transform(new Vector2((float)mouseX.Value, (float)mouseY.Value), chosenShape.Rotation));
            }
        }
    }

    private ChosenShape? chosenShape { get; set; }

    public void SetChosenShape(ChosenShape? shape)
    {
        chosenShape = shape;
    }

    private List<GameShape> AddedShapes = new();
    private Stack<(ChosenShape Shape, Vector2 Position)> _bodiesToAdd = new();

    private Task MouseDownCanvas(MouseEventArgs e)
    {
        return OnClick(e.ClientX, e.ClientY);
    }

    private Task TouchStartCanvas(TouchEventArgs e)
    {
        if (e.Touches.Any())
        {
            return OnClick(e.Touches.First().ClientX, e.Touches.First().ClientY);
        }

        return Task.CompletedTask;
    }

    private async Task OnClick(double clientX, double clientY)
    {
        if (chosenShape is not null)
        {
            var x = clientX - containerRect.X;
            var y = clientY - containerRect.Y;

            var vec = new Vector2(
                ((float)x - XOffset) / XScale,
                ((float)y - YOffset) / YScale);

            _bodiesToAdd.Push(
                (chosenShape, vec)
            );
            AddedShapes.Add(chosenShape.Shape);

            chosenShape = null;
        }
    }


    private void MouseUpCanvas(MouseEventArgs e)
    {
        //render_required = false;
        //mousedown = false;
    }

    private double? mouseX = null;
    private double? mouseY = null;

    async Task MouseMoveCanvas(MouseEventArgs e)
    {
        mouseX = e.ClientX - containerRect.X;
        mouseY = e.ClientY - containerRect.Y;
    }

    async Task MouseOutCanvas(MouseEventArgs e)
    {
        mouseX = null;
        mouseY = null;
    }
}