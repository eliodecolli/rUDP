using rUDP.Core.Models;

namespace rUDP.Core.Interfaces;

public interface IUdpClient
{
    public Task SendData(byte[] data, SendJobConfiguration job);
}
