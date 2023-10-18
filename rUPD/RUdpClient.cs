﻿using rUDP.Core.Interfaces;
using rUDP.Core.Models;
using rUDP.Core.Models.Jobs;
using rUDP.Core.Utils;
using System.Net.Sockets;
using System.Net;
using rUDP.Core.Enums;
using rUDP.Core.Stores;
using rUDP.Core;

namespace rUDP;

public sealed class RUdpClient : IUdpClient, IUdpServer
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
    }

    private async Task RetryFragment(Guid id, int fragmentNumber)
    {
        _logger.Debug($"Retrying to send fragment {fragmentNumber} for Job {id}.");
        var job = _jobsStore.GetJobStatus(id);
        var fragment = _jobFragmentsStore.GetFragment(id, fragmentNumber);

        await _udpClient.SendAsync(fragment.Buffer, fragment.Buffer.Length, job.Destination);
    }

    private async Task HandleIncomingJobResponse(byte[] packet, IPEndPoint source)
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
                jobStatus.JobStatus = JobStatus.Completed;

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

    private async Task HandleIncomingFragment(byte[] packet, IPEndPoint source)
    {
        _logger.Debug($"Handling UDP Fragment from {source} ({packet.Length} bytes)");

        var fragment = Utils.ParseUdpFragment(packet);

    }

    private void HandleJobTimeout(object? state)
    {
        var jobId = (Guid?)state;
        if(!jobId.HasValue)
        {
            _logger.Error("Error while calling job timeout for NULL Job Id.");
            return;
        }

        _logger.Debug($"Job {jobId} has timed out.");

        var job = _jobsStore.GetJobStatus(jobId.Value);
        job.JobStatus = JobStatus.Timedout;
    }

    private void InnerRun()
    {
        while(true)
        {
            IPEndPoint? source = null;
            var packet = _udpClient.Receive(ref source);
            if(source is not null)
            {
                _logger.Info($"UDP Client: Received {packet.Length} bytes from {source}");
                var udpPacket = Utils.ParsePacket(packet);
                switch(udpPacket.Header)
                {
                    case UdpHeader.JobResponse:
                        Task.Run(async () => await HandleIncomingJobResponse(packet, source));
                        break;
                    case UdpHeader.UdpFragment:
                        Task.Run(async () => await HandleIncomingFragment(packet, source));
                        break;
                    default:
                        _logger.Error("Invalid packet header.");
                        break;
                }
            }
            else
            {
                _logger.Error("Invalid source from UDP Client?");
            }
        }
    }

    public async Task SendData(byte[] data, SendJobConfiguration job)
    {
        var newJob = new SendJob()
        {
            JobId = job.JobId,
            JobStatus = JobStatus.Running,
            NAcksNumbers = new List<int>(),
            Destination = job.Destination,
            AcksNumbers = new List<int>(),
            TimeoutTime = DateTime.Now.AddMilliseconds(job.TimeoutInterval + 2)   // add a 2ms as a buffer
        };

        _jobsStore.CreateNewJob(newJob, await Utils.FragmentData(data, job.FragmentSize, job.JobId));

        new Timer(HandleJobTimeout, job.JobId, DateTime.Now.Subtract(newJob.TimeoutTime).Milliseconds, Timeout.Infinite);

        var fragments = _jobFragmentsStore.GetFragments(job.JobId);
        foreach (var fragment in fragments)
        {
            var status = _jobsStore.GetJobStatus(fragment.JobId);
            if(status.JobStatus == JobStatus.Running)
            {
                _logger.Info($"Sending fragment {fragment.FragmentNumber} to {newJob.Destination} for Job {newJob.JobId} ({job.FragmentSize} bytes).");
                await _udpClient.SendAsync(fragment.Buffer, fragment.Buffer.Length, job.Destination);
            }
        }
    }

    public void Start()
    {
        // start waiting for incoming notifications
        Task.Run(InnerRun);
    }
}
