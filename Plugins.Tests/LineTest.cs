using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugins;
using System.Collections.Generic;
using Plugins.WKT;

namespace Plugins.Tests
{
    [TestFixture]
    internal class LineTest
    {
        private static readonly object[] LineData =
        {
            "(534708.506 5856671.649,534709.175 5856670.851)",
            "(534710.865 5856672.269,534708.572 5856670.345)"
        };
        [Test]
        [TestCaseSource(nameof(LineData))]
        public void Line_CreateValid_ObjectReturn(string source)
        {
            LineString actual = new LineString(source);
            Assert.True(actual.ToString().Equals(source));
        }
    }
}
