using rUDP.Core.Models;

namespace rUDP.Core.Interfaces;

public interface IJobFragmentsStore
{
    void RegisterFragments(Guid jobId, List<UdpFragment> fragments);

    void ClearFragments(Guid jobId);

    UdpFragment GetFragment(Guid jobId, int fragmentNumber);

    List<UdpFragment> GetFragments(Guid jobId);
}
