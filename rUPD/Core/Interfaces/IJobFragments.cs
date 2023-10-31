using rUDP.Core.Models;
using rUDP.Core.Types;

namespace rUDP.Core.Interfaces;

public interface IJobFragments
{
    bool RegisterFragment(UdpFragment fragment);

    bool IsCompleted();

    IEnumerable<int> ReportMissingFragments();

    int GetCurrentNumberOfFragments();

    UdpBuffer GenerateLatestResult(bool returnIfIncomplete = true);
}
