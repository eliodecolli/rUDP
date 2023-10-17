namespace rUPD.Core.Models;

public class SendJobStatus
{
    public Guid JobId { get; set; }

    public bool IsCompleted { get; set; }

    public int TotalFragments { get; set; }

    public List<int> AcksNumbers { get; set; }

    public List<int> NAcksNumbers { get; set; }
}
