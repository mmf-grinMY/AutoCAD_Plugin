using Plugins.WKT;

namespace Plugins.Tests
{
    internal class TestMultiLineStrings
    {
        private static readonly object[] MultiLineStringsData =
        new object[]
        {
            "MULTILINESTRING((534708.506 5856671.649,534709.175 5856670.851))",
            "MULTILINESTRING((531101.482 5857970.097,531101.482 5857972.097),(531102.114 5857974.097,531096.114 5857974.097),(531098.046 5857973.579,531096.114 5857974.097,531098.046 5857974.615),(531096.747 5857970.097,531096.747 5857972.097),(531101.482 5857970.097,531101.482 5857967.097),(531101.482 5857969.347,531100.732 5857969.347),(531101.482 5857968.597,531099.982 5857968.597),(531101.482 5857967.847,531100.732 5857967.847),(531096.747 5857970.097,531096.747 5857967.097),(531096.747 5857969.347,531097.497 5857969.347),(531096.747 5857968.597,531098.247 5857968.597),(531096.747 5857967.847,531097.497 5857967.847))"
        };
        [Test]
        [TestCaseSource(nameof(MultiLineStringsData))]
        public void MultiLineStrings_Constructor_GoodCreate(string source)
        {
            MultiLineStrings strings = new MultiLineStrings(source);
            Assert.True(strings.ToString().Equals(source));
        }
    }
}
