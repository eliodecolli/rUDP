using rUDP.Core.Enums;
using rUDP.Core.Interfaces;
using System.Net;

namespace rUDP.Core.Models.Jobs
{
    public class InboundJob : BaseJob
    {
        internal InboundJob(Guid jobId)
        {
            // the job data is updated once we receive packets
            JobId = jobId;
        }

        public JobStatus JobStatus { get; set; }

        public IPEndPoint? Source { get; set; }

        public IJobFragments? Fragments { get; set; }
    }
}
