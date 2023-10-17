using rUDP.Core.Interfaces;
using rUDP.Core.Models;

namespace rUDP.Core.Stores;

public sealed class InMemorySendJobStore : ISendJobStore
{
    private readonly Dictionary<Guid, SendJobStatus> _runningJobs;
    private readonly IJobFragmentsStore _jobFragmentsStore;

    public InMemorySendJobStore(IJobFragmentsStore fragmentsStore)
    {
        _runningJobs = new Dictionary<Guid, SendJobStatus>();
        _jobFragmentsStore = fragmentsStore;
    }

    public void CreateNewJob(SendJobStatus job, List<UdpFragment> fragments)
    {
        _runningJobs.Add(job.JobId, job);
        _jobFragmentsStore.RegisterFragments(job.JobId, fragments);
    }

    public void DeleteJob(Guid id)
    {
        _runningJobs.Remove(id);
        _jobFragmentsStore.ClearFragments(id);
    }

    public SendJobStatus GetJobStatus(Guid id)
    {
        return _runningJobs[id];
    }

    public void UpdateJob(Guid id, SendJobStatus job)
    {
        _runningJobs[id] = job;

        if(job.IsCompleted)
        {
            _jobFragmentsStore.ClearFragments(id);
        }
    }
}
