/*
    File Name: BaseUdpChannel.cs
    Author: Elio Decolli
    Last Update: 31/10/2023
    Purpose:
                Represents the base class for a reliable UDP Channel.
 
 */

using rUDP.Core.Enums;
using rUDP.Core.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace rUDP.Core.Models.Channel;

public abstract class BaseUdpChannel
{
    public BaseUdpChannel(ILogger logger)
        => _logger = logger;

    protected readonly ILogger _logger;

    protected Socket _socket;

    public IPEndPoint Source { get; set; }

    public IPEndPoint Destination { get; set; }

    public Guid JobId { get; set; }

    public void CloseChannel(ChannelCloseReason reason)
    {

    }

    public virtual void OnChannelClosed(ChannelCloseReason reason)
    {
        _logger.Info($"Channel for {JobId} closed: {Enum.GetName(reason)}");
    }
}
