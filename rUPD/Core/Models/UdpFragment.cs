namespace rUDP.Core.Models;

public sealed class UdpFragment
{
    public UdpFragment(Guid jobId, int fragmentNumber, int totalFragments, int totalLength, byte[] data, byte[]? signature = null)
    {
        JobId = jobId;
        FragmentNumber = fragmentNumber;
        TotalFragments = totalFragments;
        TotalLength = totalLength;
        Signature = signature;

        // Structure of a Fragment:
        // JobId -> String
        // TotalFragments -> Int32
        // FragmentNumber -> Int32
        // Length -> Int32
        // Data -> Byte[]
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);
        writer.Write(jobId.ToString());
        writer.Write(totalFragments);
        writer.Write(totalLength);
        writer.Write(fragmentNumber);
        writer.Write(data.Length);
        writer.Write(data);
        writer.Flush();

        Buffer = memoryStream.ToArray();
    }

    public Guid JobId { get; private set; }

    public int FragmentNumber { get; private set; }

    public int TotalFragments { get; private set; }

    public int TotalLength { get; private set; }

    public byte[] Buffer { get; internal set; }

    public byte[]? Signature { get; private set; }
}
