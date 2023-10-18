using rUDP.Core.Interfaces;

namespace rUDP.Core.Models.Jobs
{
    public class ReceiveJob : BaseJob
    {
        public IJobFragments Fragments { get; set; }
    }
}
