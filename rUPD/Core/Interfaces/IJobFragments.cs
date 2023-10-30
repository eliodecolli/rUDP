using rUDP.Core.Models;
using rUDP.Core.Types;

namespace rUDP.Core.Interfaces;

public interface IJobFragments
{
    bool RegisterFragment(UdpFragment fragment);

    IEnumerable<int> ReportMissingFragments();

    int GetCurrentNumberOfFragments();

    UdpBuffer GenerateLatestResult(bool returnIfIncomplete = true);
}
