using rUDP.Core.Models;
using rUDP.Core.Stores;
using rUDP.Core.Utils;

namespace rUDP.Tests
{
    public class StoresShould
    {
        [Fact]
        public async Task ReturnCorrectBufferedUdpPacket()
        {
            const int packetLength = 128;
            const int fragmentSize = 17;

            var data = new byte[packetLength];
            var totalFragments = (int)Math.Ceiling((double)packetLength / (double)fragmentSize);  // some very odd combinations

            new Random().NextBytes(data);

            var jobFragments = new InMemoryJobFragments(totalFragments, packetLength, fragmentSize);

            for(int i = 0; i < totalFragments; i++)
            {
                var temp = new byte[fragmentSize];
                var sourceIndex = i * fragmentSize;
                if(data.Length - 1 < sourceIndex + fragmentSize)
                {
                    var diff = (sourceIndex + fragmentSize) - data.Length;
                    var newLen = fragmentSize - diff;
                    var newIndex = data.Length - newLen;

                    Array.Copy(data, newIndex, temp, 0, newLen);
                    Array.Resize(ref temp, newLen);
                }
                else
                {
                    Array.Copy(data, sourceIndex, temp, 0, fragmentSize);
                }

                var udpFragment = new UdpFragment(Guid.Empty, i + 1, packetLength, temp);

                var result = jobFragments.RegisterFragment(udpFragment);
                Assert.True(result);

                if(i % 5 == 0)
                {
                    var currentBuffer = jobFragments.GenerateLatestResult().GetBytes();
                    var areCompletelyZeros = Array.FindAll(currentBuffer, b => b == 0x00).Length == packetLength;

                    Assert.Equal(packetLength, currentBuffer.Length);
                    Assert.False(areCompletelyZeros);
                }
            }

            var completedBuffer = jobFragments.GenerateLatestResult();
            Assert.True(completedBuffer.IsComplete);
            Assert.Equal(data, completedBuffer.GetBytes());
        }
    }
}