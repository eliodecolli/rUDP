using rUDP.Core.Enums;
using System.Net;

namespace rUDP.Core.Models.Jobs;

public class SendJob : BaseJob
{
    public IPEndPoint Destination {  get; init; }

    public DateTime TimeoutTime { get; init; }

    public JobStatus JobStatus { get; set; }

    public List<int> AcksNumbers { get; init; }

    public List<int> NAcksNumbers { get; init; }
}
