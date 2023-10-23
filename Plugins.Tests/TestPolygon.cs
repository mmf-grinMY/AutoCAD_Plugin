using Plugins.WKT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.Tests
{
    [TestFixture]
    internal class TestPolygon
    {
        [Test]
        [TestCase("POLYGON((531066.938 5857970.097, 531108.938 5857970.097, 531108.938 5857967.097, 531066.938 5857967.097, 531066.938 5857970.097))")]
        public void TestPolygon_ctor_good(string source)
        {
            Polygon polygon = new Polygon(source);
            Assert.True(polygon.ToString().Equals(source.Replace(", ", ",")));
        }
    }
}
