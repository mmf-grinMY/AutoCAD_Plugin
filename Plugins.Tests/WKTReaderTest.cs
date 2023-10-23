using Plugins.WKT;

namespace Plugins.Tests
{
    public class WKTReaderTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [TestCase("MULTILINESTRING((0.000 0.000,1000.000 1000.000))")]
        public void Read_IsValidReadingMultiLineStrings(string wkt)
        {
            MultiLineStrings multi = Reader.Read(wkt, DrawType.Polyline) as MultiLineStrings;
            //Assert.AreEqual(DrawType.Polyline, drawType);
            Assert.AreEqual(1, multi.Lines.Count);
        }
    }
}