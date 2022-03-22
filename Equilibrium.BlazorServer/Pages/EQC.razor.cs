using System.Numerics;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.World;
using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using DotNetObjectReference = Microsoft.JSInterop.DotNetObjectReference;


namespace Equilibrium.BlazorServer.Pages;

public partial class EQC
{

    private ElementReference container;

    private Canvas helper_canvas;
    private const int _physicsIterations = 8;
    private Context2D _canvasContext;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        World = new World(new Vector2(0, 10));
        Bodies.Add(World.CreateRectangle(BodyType.Static, new Vector2(0 + XOffset, 300 + YOffset), 400 * XScale, 10 * YScale));
    }

    public World World { get; private set; }

    public List<Body> Bodies { get; private set; } = new ();

    private class CanvasPosition
    {
        public double Left { get; set; }
        public double Top { get; set; }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _canvasContext = await helper_canvas.GetContext2DAsync();

            await _canvasContext.LineWidthAsync(0.1);
            await _canvasContext.FontAsync("20px Comic Sans MS");


            //this._canvasContext = await this.helper_canvas .CreateCanvas2DAsync();
            await JsRuntime.InvokeAsync<object>("initGame", new[] { DotNetObjectReference.Create(this) as object });

            
        }
    }

    

    // needed to calculate fps
    float _lastTimestamp = 0;
    float _fps = 0;

    private float XScale = 1;
    private float YScale = 1;
    private float XOffset = 0;
    private float YOffset = 0;

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

        await batch.ResetTransformAsync();

        await batch.ClearRectAsync(0, 0, width, height);


        await batch.FillStyleAsync("green");
        await batch.StrokeStyleAsync("red");

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

        World.Step(dt / 1000, _physicsIterations, _physicsIterations);

        while (_bodiesToAdd.TryPop(out var position))
        {
            var newBody = World.CreateCircle(position, 10);
            Bodies.Add(newBody);
        }
        foreach (var body in Bodies)
        {
            await batch.DrawBodyAsync(body);
        }

        await batch.ResetTransformAsync();
        await batch.FillStyleAsync("black");
        await batch.FillTextAsync($"FPS: {(int)_fps}", 10, 50);
    }

    private Stack<Vector2> _bodiesToAdd = new();

    private async Task MouseDownCanvas(MouseEventArgs e)
    {
        var p = await JsRuntime.InvokeAsync<CanvasPosition>("eval", $"let e = document.querySelector('[_bl_{container.Id}=\"\"]'); e = e.getBoundingClientRect(); e = {{ 'Left': e.x, 'Top': e.y }}; e");
        var (canvasx, canvasy) = (p.Left, p.Top);

        //render_required = false;
        var x = e.ClientX - canvasx;
        var y = e.ClientY - canvasy;

        _bodiesToAdd.Push(new Vector2(
            ((float) x- XOffset) / XScale , 
            ((float)y - YOffset) / YScale ));

        
        //this.mousedown = true;
    }

    private void MouseUpCanvas(MouseEventArgs e)
    {
        //render_required = false;
        //mousedown = false;
    }

    async Task MouseMoveCanvasAsync(MouseEventArgs e)
    {
        //render_required = false;
        //if (!mousedown)
        //{
        //    return;
        //}
        //mousex = e.ClientX - canvasx;
        //mousey = e.ClientY - canvasy;
        //await DrawCanvasAsync(mousex, mousey, last_mousex, last_mousey, clr);
        //last_mousex = mousex;
        //last_mousey = mousey;
    }
}