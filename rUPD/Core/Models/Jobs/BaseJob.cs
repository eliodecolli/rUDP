namespace rUDP.Core.Models.Jobs;

public abstract class BaseJob
{
    public Guid JobId { get; set; }

    public int TotalFragments { get; set; }
}
