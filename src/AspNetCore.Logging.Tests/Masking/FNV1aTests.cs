using Codestellation.AspNetCore.Logging.Masking;
using NUnit.Framework;

namespace Codestellation.AspNetCore.Logging.Tests.Masking
{
    [TestFixture]
    public class FNV1aTests
    {
        [Test]
        public void CalcHash()
        {
            ulong hash = FNV1a.Hash("Lorem");

            Assert.That(hash, Is.EqualTo(1789342528));
        }
    }
}