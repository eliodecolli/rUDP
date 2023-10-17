using rUPD.Core.Models;

namespace rUPD.Core.Interfaces;

public interface IUdpClient
{
    public Task SendData(byte[] data, SendJobConfiguration job);
}
