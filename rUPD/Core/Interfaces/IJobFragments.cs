using rUDP.Core.Models;

namespace rUDP.Core.Interfaces;

public interface IJobFragments
{
    bool RegisterFragment(UdpFragment fragmemt);

    IEnumerable<int> ReportMissingFragments();

    byte[] GenerateLatestResult();
}
