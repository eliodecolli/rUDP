using System.Net;

namespace rUDP.Core.Models;

public sealed class SendJobConfiguration
{
    public SendJobConfiguration(Guid jobId, int fragmentSize, IPEndPoint destination, long timeoutInterval, bool signatureCheck)
    {
        JobId = jobId;
        FragmentSize = fragmentSize;
        Destination = destination;
        TimeoutInterval = timeoutInterval;
        SignatureCheck = signatureCheck;
    }

    public Guid JobId { get; private set; }

    public int FragmentSize { get; private set; }

    public IPEndPoint Destination { get; private set; }

    public long TimeoutInterval { get; private set; }

    public bool SignatureCheck { get; private set; }
}
