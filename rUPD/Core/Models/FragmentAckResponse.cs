using rUPD.Core.Enums;

namespace rUPD.Core.Models;

public sealed class FragmentAckResponse : JobResponse
{
    public FragmentAckResponse(JobResponseType type, Guid jobId, int fragmentNumber) : base(type, jobId)
    {
        FragmentNumber = fragmentNumber;
    }

    public int FragmentNumber { get; private set; }
}
