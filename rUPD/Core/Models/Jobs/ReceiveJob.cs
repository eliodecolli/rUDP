using rUDP.Core.Enums;
using rUDP.Core.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace rUDP.Core.Models.Jobs
{
    public class ReceiveJob : BaseJob
    {
        public IJobFragments Fragments { get; set; }

        
    }
}
