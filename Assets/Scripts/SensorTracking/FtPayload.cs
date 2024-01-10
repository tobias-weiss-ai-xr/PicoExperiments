using System;

public class FtPayload
{
    public DateTime Timestamp { get; set; }
    public string State { get; set; }
    public float[] Weights { get; set; }
    public FtPayload(string state, float[] weights)
    {
        Timestamp = DateTime.Now;
        State = state;
        Weights = weights;
    }

}
