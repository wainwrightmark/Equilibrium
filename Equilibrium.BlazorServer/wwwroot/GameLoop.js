// the main gameloop. Will be called 60 times per second by requestAnimationFrame
function gameLoop(timeStamp) {
    window.requestAnimationFrame(gameLoop);
    game.instance.invokeMethodAsync('GameLoop', timeStamp, game.canvases[0].width, game.canvases[0].height);
}

// will be called by blazor to initialize the game and register the game instance.
window.initGame = (instance) => {
    var canvasContainer = document.getElementById('canvasContainer'),
        canvases = canvasContainer.getElementsByTagName('canvas') || [];

    window.game = {
        instance: instance,
        canvases: canvases
    };

    // always keep focus
    window.game.canvases[0].onblur = (e) => {
        window.game.canvases[0].focus();
    };
    window.game.canvases[0].tabIndex = 0;
    window.game.canvases[0].focus();

    window.requestAnimationFrame(gameLoop);
};