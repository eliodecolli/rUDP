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
    }

    private void HandleIncomingPacket(byte[] packet, IPEndPoint source)
    {
        _logger.Debug($"Handling packet from {source}");

        var fragmentResult = Utils.ParseJobResponse(packet);

        _logger.Info($"Received {Enum.GetName(fragmentResult.ResponseType)} for Job Id {fragmentResult.JobId}");

        var jobStatus = _jobsStore.GetJobStatus(fragmentResult.JobId);
        switch(fragmentResult.ResponseType)
        {
            case JobResponseType.JobEnd:
                jobStatus.IsCompleted = true;

                _logger.Debug($"Finished Job {fragmentResult.JobId}");
                break;
            case JobResponseType.FragmentAck:
                var fragmentAck = (FragmentAckResponse)fragmentResult;
                jobStatus.AcksNumbers.Add(fragmentAck.FragmentNumber);

                _logger.Debug($"Received ACK for Job {fragmentResult.JobId}");
                break;
            case JobResponseType.FragmentNAck:
                var fragmentNack = (FragmentAckResponse)fragmentResult;
                jobStatus.NAcksNumbers.Add(fragmentNack.FragmentNumber);

                _logger.Warn($"Received NACK for Fragment {fragmentNack.FragmentNumber} Job {fragmentNack.JobId}");
                break;
        }

        _jobsStore.UpdateJob(jobStatus.JobId, jobStatus);
    }

    private void InnerRun()
    {
        while(true)
        {
            IPEndPoint? source = null;
            var packet = _udpClient.Receive(ref source);
            if(source is not null)
            {
                Task.Run(() => HandleIncomingPacket(packet, source));
            }
            else
            {
                _logger.Error("Invalid source from UDP Client?");
            }
        }
    }

    public async Task SendData(byte[] data, SendJobConfiguration job)
    {
        var fragments = await Utils.FragmentData(data, job.FragmentSize, job.JobId);

        var newJob = new SendJobStatus()
        {
            IsCompleted = false,
            JobId = job.JobId,
            NAcksNumbers = new List<int>(),
            TotalFragments = 0
        };

        _jobsStore.CreateNewJob(newJob, fragments);
    }
}
