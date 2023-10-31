namespace rUDP.Core.Enums;

public enum UdpHeader : byte
{
    UdpFragment = 0x00,
    JobResponse = 0x01,
    CloseChannel = 0x02
}
