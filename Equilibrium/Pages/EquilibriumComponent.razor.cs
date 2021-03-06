using Excubo.Generators.Blazor.ExperimentalDoNotUseYet;
using Majorsoft.Blazor.Components.Common.JsInterop.GlobalMouseEvents;
using Majorsoft.Blazor.Components.Common.JsInterop.Resize;
using Majorsoft.Blazor.Extensions.BrowserStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using DotNetObjectReference = Microsoft.JSInterop.DotNetObjectReference;


namespace Equilibrium.Pages;

public partial class EquilibriumComponent
{
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public IResizeHandler ResizeHandler { get; set; } = null!;

    [Inject] public ILocalStorageService LocalStorageService { get; set; } = null!;

    private ElementReference _container;
    private Canvas _canvas = null!;
    private Context2D _canvasContext = null!;

    private TransientState TransientState { get; } = new();

    private GameState GameState { get; set; } = new(Level.Basic);


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var keys = await LocalStorageService.GetAllKeysAsync().ToListAsync();

            var levels = await Task.WhenAll(keys.Where(x => x.StartsWith("Level"))
                .Select(k => LocalStorageService.GetItemAsync<UserLevel>(k)));

            SavedLevels = levels.Prepend(BasicLevel).ToList();


            _canvasContext = await _canvas.GetContext2DAsync();

            var pageSize = await ResizeHandler.GetPageSizeAsync();
            
            await OnResize(new ResizeEventArgs(){EventId = "Init", Height = pageSize.Height, Width = pageSize.Width});
            await ResizeHandler.RegisterPageResizeAsync(OnResize);
            GameState.Restart(TransientState, new Random());
            GameState.StateChanged += delegate { StateHasChanged(); };

            await JsRuntime.InvokeAsync<object>("initGame",
                new[] { DotNetObjectReference.Create(this) as object });

            StateHasChanged();
            //GameState.DragFirstBody(TransientState);
        }
    }

    private readonly record struct CanvasPosition(double Left, double Top);

    private CanvasPosition _canvasPosition;

    private ProgressComponent _progressComponent;

    private double _windowWidth = Constants.GameWidth;
    private double _windowHeight = Constants.GameHeight;

    public double _canvasWidth => Constants.GameWidth;
    public double _canvasHeight => Math.Min(Constants.GameHeight, _windowHeight - 5);


    protected  Task OnResize(ResizeEventArgs rea)
    {
        return OnResize(rea.Width, rea.Height);
    }

    private async Task OnResize(double width, double height)
    {
        _windowWidth = width;
        _windowHeight = height;

        _canvasPosition = await JsRuntime.InvokeAsync<CanvasPosition>(
            "eval",
            $"let e = document.querySelector('[_bl_{_container.Id}=\"\"]'); e = e.getBoundingClientRect(); e = {{ 'Left': e.x, 'Top': e.y }}; e");

        StateHasChanged();
    }

    private readonly object _drawing = new();

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
        if (Monitor.TryEnter(_drawing))
        {
            await using var batch = _canvasContext.CreateBatch();
            await GameState.StepAndDraw(timeStamp, batch, width, height, TransientState);
            _progressComponent.StateChanged();
            Monitor.Exit(_drawing);

            if (GameState.IsWin && !UserLevel.IsBeaten)
            {
                UserLevel = UserLevel with { IsBeaten = true };
                await SaveLevel();
            }
        }
    }

    private void OnMouseDown(MouseEventArgs e)
    {
        if(e.Button != 0) return;
        
        var x = e.ClientX - _canvasPosition.Left;
        var y = e.ClientY - _canvasPosition.Top;

        GameState.MaybeStartDrag(MouseDragIdentifier.Instance, (float)x, (float)y, TransientState);

    }
    
    private void OnTouchStart(TouchEventArgs e)
    {
        foreach (var touchPoint in e.ChangedTouches)
        {
            var x = touchPoint.ClientX - _canvasPosition.Left;
            var y = touchPoint.ClientY - _canvasPosition.Top;

            GameState.MaybeStartDrag(new TouchDragIdentifier(touchPoint.Identifier), (float)x, (float)y, TransientState);
        }
    }

    private void OnTouchMove(TouchEventArgs e)
    {
        foreach (var touchPoint in e.ChangedTouches)
        {
            var x = touchPoint.ClientX - _canvasPosition.Left;
            var y = touchPoint.ClientY - _canvasPosition.Top;

            GameState.OnDragMove(new TouchDragIdentifier(touchPoint.Identifier),
                (float)x, (float)y, TransientState);
        }
    }

    private void OnMouseMove(MouseEventArgs e)
    {
        var x = e.ClientX - _canvasPosition.Left;
        var y = e.ClientY - _canvasPosition.Top;

        GameState.OnDragMove(MouseDragIdentifier.Instance,(float)x, (float)y, TransientState);

    }
    
    private void OnMouseOut(MouseEventArgs e)
    {
        GameState.EndDrag(MouseDragIdentifier.Instance,TransientState);
    }

    private void OnTouchEnd(TouchEventArgs e)
    {
        foreach (var touchPoint in e.ChangedTouches)
        {
            GameState.EndDrag(new TouchDragIdentifier(touchPoint.Identifier), TransientState);   
        }

        if (!e.Touches.Any())//Incase any drag end events get dropped
            GameState.EndAllTouchDrags(TransientState);
    }

    private void OnTouchCancel(TouchEventArgs e)
    {
        foreach (var touchPoint in e.ChangedTouches)
        {
            GameState.EndDrag(new TouchDragIdentifier(touchPoint.Identifier), TransientState);   
        }
    }
    
    private void OnMouseUp(MouseEventArgs e)
    {
        if(e.Button != 0) return;
        GameState.EndDrag(MouseDragIdentifier.Instance,TransientState);
    }

    private void OnMouseWheel(WheelEventArgs obj)
    {
        var positive = obj.DeltaX + obj.DeltaY + obj.DeltaZ > 0;

        GameState.RotateDragged(positive? -1 : 1 , TransientState);
    }

    private void OnKeyPress(KeyboardEventArgs obj)
    {
        if (obj.Key is "q" or "Q" )
        {
            GameState.RotateDragged(-1 , TransientState);
        }
        else if (obj.Key is "e" or "E" )
        {
            GameState.RotateDragged(1 , TransientState);
        }
    }
}