/*
    File Name: OutboundJob.cs
    Author: Elio Decolli
    Last Update: 31/10/2023
    Purpose:
                Contains the details needed to complete a reliable packet transfer.
 
 */

using rUDP.Core.Enums;
using System.Net;

namespace rUDP.Core.Models.Jobs;

public class OutboundJob : BaseJob
{
    internal OutboundJob(Guid jobId, IPEndPoint destination, int timeout, IReadOnlyList<UdpFragment> fragments)
    {
        JobId = jobId;
        Destination = destination;
        Timeout = timeout;
        TotalFragments = fragments.Count;
        JobStatus = JobStatus.Initialized;
        JobFragments = fragments;
        Acknowledged = new HashSet<int>();
        NotAcknowledged = new HashSet<int>();
    }

    public int Timeout { get; init; }

    public IPEndPoint Destination {  get; init; }

    public JobStatus JobStatus { get; set; }

    public IReadOnlyList<UdpFragment> JobFragments { get; init; }

    public HashSet<int> Acknowledged { get; init; }

    public HashSet<int> NotAcknowledged { get; init; }
}
