namespace rUPD.Core.Models;

public sealed class UdpFragment
{
    public UdpFragment(Guid jobId, int fragmentNumber, int totalFragments, byte[] buffer, byte[]? signature)
    {
        JobId = jobId;
        FragmentNumber = fragmentNumber;
        TotalFragments = totalFragments;
        Buffer = buffer;
        Signature = signature;
    }

    public Guid JobId { get; private set; }

    public int FragmentNumber { get; private set; }

    public int TotalFragments { get; private set; }

    public byte[] Buffer { get; private set; }

    public byte[]? Signature { get; private set; }
}
