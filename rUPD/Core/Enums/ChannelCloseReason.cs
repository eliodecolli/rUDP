namespace rUDP.Core.Enums;

public enum ChannelCloseReason : byte
{
    TransferComplete,

    TransferRefused,

    TransferTimedout,

    Unknown
}
