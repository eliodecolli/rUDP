using rUDP.Core.Interfaces;
using rUDP.Core.Models;

namespace rUDP.Core.Stores;

public sealed class InMemoryFragmentsStore : IJobFragmentsStore
{
    private readonly Dictionary<Guid, List<UdpFragment>> _store;

    public InMemoryFragmentsStore()
    {
        _store = new Dictionary<Guid, List<UdpFragment>>();
    }

    public void ClearFragments(Guid jobId)
    {
        _store.Remove(jobId);
    }

    public UdpFragment GetFragment(Guid jobId, int fragmentNumber)
    {
        return _store[jobId][fragmentNumber - 1];
    }

    public IJobFragments GetReceiveFragments(Guid jobId)
    {
        return new InMemoryJobFragments(1, 0, 0);
    }

    public void RegisterFragments(Guid jobId, List<UdpFragment> fragments)
    {
        _store.Add(jobId, fragments);
    }
}
