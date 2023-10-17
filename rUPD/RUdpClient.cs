using rUPD.Core.Interfaces;
using rUPD.Core.Models;
using rUDP.Core.Utils;
using System.Net.Sockets;
using System.Net;
using rUDP.Core.Interfaces;
using rUDP.Core.Enums;
using rUDP.Core.Stores;
using rUDP.Core;

namespace rUDP;

public sealed class RUdpClient : IUdpClient
{
    private readonly UdpClient _udpClient;

    private readonly ILogger _logger;

    private readonly ISendJobStore _jobsStore;

    private readonly IJobFragmentsStore _jobFragmentsStore;

    public RUdpClient(int port)
    {
        _jobFragmentsStore = new InMemoryFragmentsStore();
        _jobsStore = new InMemorySendJobStore(_jobFragmentsStore);

        _logger = new ConsoleLogger();

        _udpClient = new UdpClient(port);

        // start waiting for incoming notifications
        Task.Run(InnerRun);
    }

    private async Task RetryFragment(Guid id, int fragmentNumber)
    {
        _logger.Debug($"Retrying to send fragment {fragmentNumber} for Job {id}.");
        var job = _jobsStore.GetJobStatus(id);
        var fragment = _jobFragmentsStore.GetFragment(id, fragmentNumber);

        await _udpClient.SendAsync(fragment.Buffer, fragment.Buffer.Length, job.Destination);
    }

    private async Task HandleIncomingPacket(byte[] packet, IPEndPoint source)
    {
        _logger.Debug($"Handling packet from {source}");

        var fragmentResult = Utils.ParseJobResponse(packet);

        _logger.Info($"Received {Enum.GetName(fragmentResult.ResponseType)} for Job Id {fragmentResult.JobId}");

        var jobStatus = _jobsStore.GetJobStatus(fragmentResult.JobId);

        if(jobStatus.Destination != source)
        {
            _logger.Error($"Invalid packet source for Job {jobStatus.JobId}: Was expecting to get ACK from '{jobStatus.Destination}' but we got it from '{source}'");
            return;
        }

        switch(fragmentResult.ResponseType)
        {
            case JobResponseType.JobEnd:
                jobStatus.IsCompleted = true;

                _logger.Debug($"Finished Job {fragmentResult.JobId}");
                break;
            case JobResponseType.FragmentAck:
                var fragmentAck = (FragmentAckResponse)fragmentResult;
                jobStatus.AcksNumbers.Add(fragmentAck.FragmentNumber);

                if(jobStatus.NAcksNumbers.Contains(fragmentAck.FragmentNumber))
                {
                    var index = jobStatus.NAcksNumbers.IndexOf(fragmentAck.FragmentNumber);
                    jobStatus.NAcksNumbers.RemoveAt(index);
                }

                _logger.Debug($"Received ACK for Job {fragmentResult.JobId}");
                break;
            case JobResponseType.FragmentNAck:
                var fragmentNack = (FragmentAckResponse)fragmentResult;
                jobStatus.NAcksNumbers.Add(fragmentNack.FragmentNumber);

                _logger.Warn($"Received NACK for Fragment {fragmentNack.FragmentNumber} Job {fragmentNack.JobId}");

                await RetryFragment(fragmentNack.JobId, fragmentNack.FragmentNumber);
                break;
        }

        _jobsStore.UpdateJob(jobStatus.JobId, jobStatus);
    }

    private void HandleJobTimeout(object? state)
    {
        var jobId = (Guid?)state;
        _logger.Debug($"Job {jobId} has timed out.");
    }

    private void InnerRun()
    {
        while(true)
        {
            IPEndPoint? source = null;
            var packet = _udpClient.Receive(ref source);
            if(source is not null)
            {
                Task.Run(async () =>  await HandleIncomingPacket(packet, source));
            }
            else
            {
                _logger.Error("Invalid source from UDP Client?");
            }
        }
    }

    public async Task SendData(byte[] data, SendJobConfiguration job)
    {
        var newJob = new SendJobStatus()
        {
            IsCompleted = false,
            JobId = job.JobId,
            NAcksNumbers = new List<int>(),
            TotalFragments = 0,
            Destination = job.Destination,
            AcksNumbers = new List<int>(),
            TimeoutTime = DateTime.Now.AddMilliseconds(job.TimeoutInterval + 2)   // add a 2ms as a buffer
        };

        _jobsStore.CreateNewJob(newJob, await Utils.FragmentData(data, job.FragmentSize, job.JobId));

        new Timer(HandleJobTimeout, job.JobId, DateTime.Now.Subtract(newJob.TimeoutTime).Milliseconds, Timeout.Infinite);

        var fragments = _jobFragmentsStore.GetFragments(job.JobId);
        foreach (var fragment in fragments)
        {
            _logger.Info($"Sending fragment {fragment.FragmentNumber} to {newJob.Destination} for Job {newJob.JobId} ({job.FragmentSize} bytes).");
            await _udpClient.SendAsync(fragment.Buffer, fragment.Buffer.Length, job.Destination);
        }
    }
}
