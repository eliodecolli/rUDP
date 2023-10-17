using rUDP.Core.Enums;

namespace rUDP.Core.Models;

public class JobResponse
{
    public JobResponse(JobResponseType responseType, Guid jobId)
    {
        ResponseType = responseType;
        JobId = jobId;
    }

    public JobResponseType ResponseType { get; private set; }

    public Guid JobId { get; private set; }
}
