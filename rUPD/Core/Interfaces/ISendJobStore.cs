using rUDP.Core.Models;
using rUDP.Core.Models.Jobs;

namespace rUDP.Core.Interfaces;

public interface ISendJobStore
{
    void CreateNewJob(OutboundJob job, List<UdpFragment> fragments);

    void DeleteJob(Guid id);

    void UpdateJob(Guid id, OutboundJob job);

    OutboundJob GetJobStatus(Guid id);
}
