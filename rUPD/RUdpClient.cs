using rUDP.Core.Interfaces;
using rUDP.Core.Models;
using rUDP.Core.Models.Jobs;
using System.Net.Sockets;
using System.Net;
using rUDP.Core.Enums;
using rUDP.Core;
using rUDP.Core.Models.Channel;

namespace rUDP;

public sealed class RUdpClient
{
    private readonly ILogger _logger;

    private readonly HashSet<int> _reservedPorts;

    private readonly object _lock = new object();

    public RUdpClient()
    {
        _reservedPorts = new HashSet<int>();
        _logger = new ConsoleLogger();
    }

    private void ReleaseChannel(int port)
    {
        if(port != -1)
        {
            lock (_lock)
            {
                _reservedPorts.Remove(port);
            }
        }
    }

    public async Task<OutboundChannel> CreateOutboundChannel(byte[] data, short fragmentSize, IPEndPoint destination, int port = -1, int timeout = 10000)
    {
        var jobId = Guid.NewGuid();
        var fragments = await Utils.FragmentData(data, fragmentSize, jobId);

        var job = new OutboundJob(jobId, destination, timeout, fragments);

        var _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        var nPort = -1;

        if(port != -1)
        {
            nPort = port;
            _reservedPorts.Add(nPort);
        }
        else
        {
            lock (_lock)
            {
                nPort = Utils.GetPort(_reservedPorts);
                _reservedPorts.Add(nPort);
            }
        }

        _socket.Bind(new IPEndPoint(IPAddress.Any, nPort));
        _socket.Listen();

        job.JobStatus = JobStatus.Initialized;

        var channel = new OutboundChannel(job, _socket, _logger, () => ReleaseChannel(nPort));

        return channel;
    }

    public InboundChannel CreateInboundChannel(int port)
    {
        var jobId = Guid.NewGuid();
        var job = new InboundJob(jobId);

        var _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, port));

        job.JobStatus = JobStatus.Initialized;

        var channel = new InboundChannel(job, _socket, _logger);

        return channel;
    }
}
