/*
    File Name: InboundChannel.cs
    Author: Elio Decolli
    Last Update: 31/10/2023
    Purpose:
                Represents an inbound channel which is used to retrieve data from a remote source.
 
 */

using rUDP.Core.Interfaces;
using rUDP.Core.Models.Jobs;
using rUDP.Core.Enums;
using rUDP.Core.Stores;
using System.Net;
using System.Net.Sockets;
using rUDP.Core.Types;

namespace rUDP.Core.Models.Channel;

public class InboundChannel : BaseUdpChannel
{
    private readonly InboundJob _job;

    public InboundChannel(InboundJob job, Socket socket, ILogger logger) : base(logger)
    {
        _job = job;
        _socket = socket;
    }

    private async Task ValidateNack(UdpFragment fragment)
    {
        if (_job.Fragments is null || _job.Source is null)
            return;

        var nextPacket = _job.Fragments.GetCurrentNumberOfFragments() + 1;
        if(fragment.FragmentNumber != nextPacket)
        {
            var nack = Utils.SerializeJobResponse(new FragmentAckResponse(JobResponseType.FragmentNAck, _job.JobId, fragment.FragmentNumber));
            var packet = Utils.SerializeUdpPacket(UdpHeader.JobResponse, nack);
            await _socket.SendToAsync(packet, _job.Source);
        }
    }

    private async Task AckFragment(UdpFragment fragment)
    {
        if (_job.Source is null)
            return;

        var ack = Utils.SerializeJobResponse(new FragmentAckResponse(JobResponseType.FragmentAck, _job.JobId, fragment.FragmentNumber));
        var packet = Utils.SerializeUdpPacket(UdpHeader.JobResponse, ack);
        await _socket.SendToAsync(packet, _job.Source);
    }

    private async Task MarkTransferComplete()
    {
        if (_job.Source is null)
            return;

        var transferEndFlag = Utils.SerializeJobResponse(new JobResponse(JobResponseType.JobEnd, _job.JobId));
        var packet = Utils.SerializeUdpPacket(UdpHeader.JobResponse, transferEndFlag);
        await _socket.SendToAsync(packet, _job.Source);
    }

    private void WrapUp(ChannelCloseReason reason)
    {
        _socket.Close();
        OnChannelClosed(reason);
    }

    private void BeginListenInternal()
    {
        _socket.Listen();
        while (_job.JobStatus == JobStatus.Running)
        {
            EndPoint source = new IPEndPoint(IPAddress.Any, 0);
            var buffer = new Span<byte>();

            var received = _socket.ReceiveFrom(buffer, ref source);
            if(received > 0)
            {
                var udpPacket = Utils.ParsePacket(buffer.ToArray());

                if(udpPacket.Header != UdpHeader.UdpFragment)
                {
                    _logger.Error($"Invalid packet type received: {Enum.GetName(udpPacket.Header)} for {_job.JobId}");
                    continue;
                }

                var fragment = Utils.ParseUdpFragment(udpPacket.Data);

                // initialize our fragments store, it's probably the first packet
                if (_job.Fragments == null)
                {
                    _job.Fragments = new InMemoryJobFragments(fragment.TotalFragments, fragment.TotalLength, /* this is probably safe*/ (short)fragment.Buffer.Length);
                    _job.Source = (IPEndPoint)source;
                }

                var registered = _job.Fragments.RegisterFragment(fragment);
                if (registered)
                {
                    // acknowledge packet
                    var ack = AckFragment(fragment);
                    ack.Start();
                    ack.Wait();

                    // validate if we missed any packet so far
                    var nack = ValidateNack(fragment);
                    nack.Start();
                    nack.Wait();

                    OnFragmentReceived(fragment);

                    if(_job.Fragments.GenerateLatestResult().IsComplete)
                    {
                        var mark = MarkTransferComplete();
                        mark.Start();
                        mark.Wait();

                        OnTransferComplete(_job.Fragments.GenerateLatestResult().Buffer.GetBuffer());
                    }
                }
                else
                {
                    _logger.Info($"Received duplicate for nr. {fragment.FragmentNumber} for {fragment.JobId}");
                }
            }
        }

        WrapUp(ChannelCloseReason.TransferComplete);
    }

    public void BeginListen()
    {
        BeginListenInternal();
    }

    public async Task BeginListenAsync()
    {
        await Task.Run(BeginListen);
    }

    public void StopListen()
    {
        if (_job.JobStatus == JobStatus.Running)
        {
            _job.JobStatus = JobStatus.Completed;
            WrapUp(ChannelCloseReason.TransferComplete);
        }
    }

    public virtual void OnFragmentReceived(UdpFragment fragment)
    {
        _logger.Info($"Received fragment: Nr. {fragment.FragmentNumber} for {fragment.JobId}");
    }

    public virtual void OnTransferComplete(byte[] data)
    {
        _logger.Info($"Finished receiving {data.Length} bytes for {_job.JobId}.");
    }
}
