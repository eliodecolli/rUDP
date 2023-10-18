namespace rUDP.Core.Enums;

public enum JobResponseType : byte
{
    FragmentAck = 0x00,
    FragmentNAck = 0x01, 
    JobEnd = 0x02
}
