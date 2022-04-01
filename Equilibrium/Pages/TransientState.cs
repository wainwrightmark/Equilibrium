namespace Equilibrium.Pages;

public class TransientState
{
    public Drag? Drag { get; set; } = null; //TODO use an id or something here

    public float LastTimestamp { get; set; } = 0;

    public int FramesPerSecond { get; set; } = 0;

    public bool DragJustEnded  {get; set;} = false;

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
        Drag = null;
        //ChosenShape = null;
    }
}