﻿using Excubo.Blazor.Canvas;
using Excubo.Blazor.Canvas.Contexts;
using Majorsoft.Blazor.Components.Common.JsInterop.Resize;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using DotNetObjectReference = Microsoft.JSInterop.DotNetObjectReference;


namespace Equilibrium.Pages;

public partial class EQC
{
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public IResizeHandler ResizeHandler { get; set; } = null!;


    private ElementReference container;
    private Canvas _canvas = null!;
    private Context2D _canvasContext = null!;

    private TransientState TransientState { get; } = new();

    private GameState GameState { get; set; } = new GameState(new LevelOne());


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _canvasContext = await _canvas.GetContext2DAsync();

            await OnResize();
            await ResizeHandler.RegisterPageResizeAsync(_ => OnResize());
            GameState.Restart(TransientState);
            GameState.StateChanged+= delegate { StateHasChanged(); };

            await JsRuntime.InvokeAsync<object>("initGame",
                new[] { DotNetObjectReference.Create(this) as object });
        }
    }

    private readonly record struct CanvasPosition(double Left, double Top);

    private CanvasPosition _canvasPosition;

    protected async Task OnResize()
    {
        _canvasPosition = await JsRuntime.InvokeAsync<CanvasPosition>(
            "eval",
            $"let e = document.querySelector('[_bl_{container.Id}=\"\"]'); e = e.getBoundingClientRect(); e = {{ 'Left': e.x, 'Top': e.y }}; e");
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

        await GameState.StepAndDraw(timeStamp, batch, width, height, TransientState);
    }


    private void MouseDownCanvas(MouseEventArgs e)
    {
        OnClick(e.ClientX, e.ClientY);
    }


    private void TouchEndCanvas(TouchEventArgs e)
    {
        var touch = e.ChangedTouches.FirstOrDefault();

        if (touch is not null)
        {
            OnClick(touch.ClientX, touch.ClientY);
        }
    }

    private void OnClick(double clientX, double clientY)
    {
        var x = clientX - _canvasPosition.Left;
        var y = clientY - _canvasPosition.Top;

        GameState.MaybeAddChosenShape((float)x, (float)y, TransientState);
    }


    private void MouseUpCanvas(MouseEventArgs e)
    {
        //render_required = false;
        //mousedown = false;
    }

    private void MouseMoveCanvas(MouseEventArgs e)
    {
        var x = e.ClientX - _canvasPosition.Left;
        var y = e.ClientY - _canvasPosition.Top;
        TransientState.MousePosition = new((float)x, (float)y);
    }

    private void TouchMoveCanvas(TouchEventArgs e)
    {
        var touch = e.ChangedTouches.FirstOrDefault();
        if (touch is not null)
        {
            var x = touch.ClientX - _canvasPosition.Left;
            var y = touch.ClientY - _canvasPosition.Top;
            TransientState.MousePosition = new((float)x, (float)y);
        }
    }

    private void MouseOutCanvas(MouseEventArgs e)
    {
        TransientState.MousePosition = null;
    }

    private void TouchLeaveCanvas(TouchEventArgs e)
    {
        TransientState.MousePosition = null;
    }
}