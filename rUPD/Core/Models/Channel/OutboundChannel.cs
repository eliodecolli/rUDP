/*
    File Name: OutboundChannel.cs
    Author: Elio Decolli
    Last Update: 31/10/2023
    Purpose:
                Represents an outbound channel which is used to send data to a remote destination.
 
 */

using rUDP.Core.Enums;
using rUDP.Core.Interfaces;
using rUDP.Core.Models.Jobs;
using rUDP.Core.Types;
using System.Net;
using System.Net.Sockets;

namespace rUDP.Core.Models.Channel;

public class OutboundChannel : BaseUdpChannel
{
    private readonly OutboundJob _job;
    internal readonly Delegates.OnChannelClose _onChannelClose;

    internal void OnChannelCloseInternal()
    {
        if( _onChannelClose != null)
        {
            _onChannelClose();  // release resources here
        }
    }

    internal OutboundChannel(OutboundJob job, Socket socket, ILogger logger, Delegates.OnChannelClose onChannelClose) : base(logger)
    {
        _job = job;
        _socket = socket;
        _onChannelClose = onChannelClose;

        Task.Run(InnerRun);
    }

    private void HandleIncomingJobResponse(byte[] packet, IPEndPoint source)
    {
        _logger.Debug($"Handling packet from {source}");

        var fragmentResult = Utils.ParseJobResponse(packet);

        _logger.Info($"Received {Enum.GetName(fragmentResult.ResponseType)} for Job Id {fragmentResult.JobId}");

        if (Destination != source)
        {
            _logger.Error($"Invalid packet source for Job {JobId}: Was expecting to get ACK from '{Destination}' but we got it from '{source}'");
            return;
        }

        switch (fragmentResult.ResponseType)
        {
            case JobResponseType.JobEnd:
                _job.JobStatus = JobStatus.Completed;
                OnChannelClosed(ChannelCloseReason.TransferComplete);

                _logger.Debug($"Finished Job {fragmentResult.JobId}");
                break;
            case JobResponseType.FragmentAck:
                var fragmentAck = (FragmentAckResponse)fragmentResult;
                _job.Acknowledged.Add(fragmentAck.FragmentNumber);

                if(_job.NotAcknowledged.Contains(fragmentAck.FragmentNumber))
                {
                    _job.NotAcknowledged.Remove(fragmentAck.FragmentNumber);
                }

                _logger.Debug($"Received ACK for Job {fragmentResult.JobId} for fragment {fragmentAck.FragmentNumber}");
                break;
            case JobResponseType.FragmentNAck:
                var fragmentNack = (FragmentAckResponse)fragmentResult;
                _job.NotAcknowledged.Add(fragmentNack.FragmentNumber);

                _logger.Warn($"Received NACK for Fragment {fragmentNack.FragmentNumber} Job {fragmentNack.JobId}");
                break;
        }
    }

    private void InnerRun()
    {
        while (_job.JobStatus == JobStatus.Running)
        {
            EndPoint source = new IPEndPoint(IPAddress.Any, 0);

            var packet = new Span<byte>();
            var incomingSocket = _socket.ReceiveFrom(packet, ref source);

            if (incomingSocket > 0)
            {
                var buffer = packet.ToArray();

                _logger.Info($"UDP Client: Received {buffer.Length} bytes from {source}");
                var udpPacket = Utils.ParsePacket(packet.ToArray());

                switch (udpPacket.Header)
                {
                    case UdpHeader.JobResponse:
                        Task.Run(() => HandleIncomingJobResponse(buffer, (IPEndPoint)source));
                        break;
                    case UdpHeader.CloseChannel:
                        _job.JobStatus = JobStatus.Completed;
                        OnChannelClosed(ChannelCloseReason.TransferRefused);
                        break;
                    default:
                        _logger.Error("Invalid packet header.");
                        break;
                }
            }
            else
            {
                _logger.Error("Invalid source from UDP Socket?");
            }
        }
    }

    private void HandleTimeout(object? state)
    {
        if(_job.JobStatus == JobStatus.Running && !_job.NotAcknowledged.Any())
        {
            _job.JobStatus = JobStatus.Timedout;
            OnChannelClosed(ChannelCloseReason.TransferTimedout);
        }
        else
        {
            // start a new timer
            BeginTimeoutHandling();
        }
    }

    private void BeginTimeoutHandling()
    {
        new Timer(HandleTimeout, null, _job.Timeout, Timeout.Infinite);
    }

    public async Task BeginSendingData()
    {
        _job.JobStatus = JobStatus.Running;

        BeginTimeoutHandling();

        while (_job.JobStatus == JobStatus.Running)
        {
            foreach (var fragment in _job.JobFragments)
            {
                if (_job.Acknowledged.Contains(fragment.FragmentNumber))
                {
                    _logger.Debug($"umm....we've already received an ack for #{fragment.FragmentNumber} for {JobId}.");
                    continue;
                }

                _logger.Info($"Sending fragment {fragment.FragmentNumber} to {_job.Destination} for Job {_job.JobId} ({fragment.Buffer.Length} bytes).");

                var buffer = Utils.SerializeFragmentPacket(fragment);
                await _socket.SendToAsync(buffer, _job.Destination);

                OnFragmentSent(fragment);

                // ..should we yield here?
            }
        }

        OnChannelCloseInternal();
    }

    public virtual void OnFragmentSent(UdpFragment fragment)
    {
        _logger.Warn("No overloads for OnFragmentSent() are set.");
    }
}
