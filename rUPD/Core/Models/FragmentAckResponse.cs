using rUDP.Core.Enums;

namespace rUDP.Core.Models;

public sealed class FragmentAckResponse : JobResponse
{
    public FragmentAckResponse(JobResponseType type, Guid jobId, int fragmentNumber) : base(type, jobId)
    {
        FragmentNumber = fragmentNumber;
    }

    public int FragmentNumber { get; private set; }
}
