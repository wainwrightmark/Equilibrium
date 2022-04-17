namespace Equilibrium.Pages;

public class TransientState
{

    public List<Drag> Drags { get; set; } = new();

    public float LastTimestamp { get; set; } = 0;

    public float YOffset { get; set; } = 0;

    public int FramesPerSecond { get; set; } = 0;

    public bool ShouldCheckForWin { get; set; } = false;

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
        Drags.Clear();
    }
}


