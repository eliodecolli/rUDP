using rUDP.Core.Models;

namespace rUDP.Core.Interfaces;

public interface ISendJobStore
{
    void CreateNewJob(SendJobStatus job, List<UdpFragment> fragments);

    void DeleteJob(Guid id);

    void UpdateJob(Guid id, SendJobStatus job);

    SendJobStatus GetJobStatus(Guid id);
}
