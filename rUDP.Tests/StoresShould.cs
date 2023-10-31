using rUDP.Core.Models;
using rUDP.Core.Stores;

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
            new Random().NextBytes(data);

            var fragments = await Core.Utils.FragmentData(data, fragmentSize, Guid.Empty);

            var jobFragments = new InMemoryJobFragments(fragments.Count, packetLength, fragmentSize);

            for(int i = 0; i < fragments.Count; i++)
            {
                var fragment = fragments[i];

                var result = jobFragments.RegisterFragment(fragment);
                Assert.True(result);

                if (i % 5 == 0)
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