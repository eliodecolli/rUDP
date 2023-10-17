using System.Net;

namespace rUDP.Core.Models;

public class SendJobStatus
{
    public Guid JobId { get; set; }

    public IPEndPoint Destination {  get; set; }

    public DateTime TimeoutTime { get; set; }

    public bool IsCompleted { get; set; }

    public List<int> AcksNumbers { get; set; }

    public List<int> NAcksNumbers { get; set; }
}
