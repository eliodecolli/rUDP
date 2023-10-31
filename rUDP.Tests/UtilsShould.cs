using rUDP.Core;

namespace rUDP.Tests
{
    public class UtilsShould
    {
        [Fact]
        public async Task FragmentDataSuccessfully()
        {
            var data = new byte[101];
            new Random().NextBytes(data);

            var fragments = await Utils.FragmentData(data, 50, Guid.NewGuid());

            Assert.Equal(3, fragments.Count);
            Assert.Equal(3, fragments[0].TotalFragments);
        }
    }
}