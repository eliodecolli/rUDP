using rUDP.Core.Models;
using rUDP.Core.Models.Jobs;

namespace rUDP.Core.Interfaces;

public interface ISendJobStore
{
    void CreateNewJob(SendJob job, List<UdpFragment> fragments);

    void DeleteJob(Guid id);

    void UpdateJob(Guid id, SendJob job);

    SendJob GetJobStatus(Guid id);
}
