public class FtPayload
{
    public string State { get; set; }
    public float[] Weights { get; set; }
    public FtPayload(string state, float[] weights)
    {
        State = state;
        Weights = weights;
    }

}
