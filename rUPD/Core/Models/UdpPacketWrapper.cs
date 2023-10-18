using rUDP.Core.Enums;

namespace rUDP.Core.Models;

public sealed class UdpPacketWrapper
{
    public UdpPacketWrapper(UdpHeader header, byte[] data)
    {
        Header = header;
        Data = data;
    }

    public UdpHeader Header { get; private set; }

    public byte[] Data { get; private set; }
}
