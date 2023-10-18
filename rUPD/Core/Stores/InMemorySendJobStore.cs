using rUDP.Core.Interfaces;
using rUDP.Core.Models.Jobs;
using rUDP.Core.Enums;
using rUDP.Core.Models;

namespace rUDP.Core.Stores;

public sealed class InMemorySendJobStore : ISendJobStore
{
    private readonly Dictionary<Guid, SendJob> _runningJobs;
    private readonly IJobFragmentsStore _jobFragmentsStore;

    public InMemorySendJobStore(IJobFragmentsStore fragmentsStore)
    {
        _runningJobs = new Dictionary<Guid, SendJob>();
        _jobFragmentsStore = fragmentsStore;
    }

    public void CreateNewJob(SendJob job, List<UdpFragment> fragments)
    {
        _runningJobs.Add(job.JobId, job);
        _jobFragmentsStore.RegisterFragments(job.JobId, fragments);
    }

    public void DeleteJob(Guid id)
    {
        _runningJobs.Remove(id);
        _jobFragmentsStore.ClearFragments(id);
    }

    public SendJob GetJobStatus(Guid id)
    {
        return _runningJobs[id];
    }

    public void UpdateJob(Guid id, SendJob job)
    {
        _runningJobs[id] = job;

        if(job.JobStatus == JobStatus.Completed ||
           job.JobStatus == JobStatus.Timedout)
        {
            _jobFragmentsStore.ClearFragments(id);
        }
    }
}
