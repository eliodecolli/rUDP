namespace rUDP.Core.Types;

public sealed class UdpBuffer
{
    public UdpBuffer(MemoryStream buffer, bool isComplete)
    {
        Buffer = buffer;
        IsComplete = isComplete;
    }

    public MemoryStream Buffer { get; private set; }

    public bool IsComplete { get; set; }

    public byte[] GetBytes()
    {
        var arr = Buffer.ToArray();
        Buffer.Seek(0, SeekOrigin.Begin);

        return arr;
    }
}
