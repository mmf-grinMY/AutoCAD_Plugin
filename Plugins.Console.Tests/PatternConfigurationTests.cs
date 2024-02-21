namespace Plugins.Console.Tests
{
    public class PatternConfigurationTests
    {
        [Test]
        [TestCaseSource(typeof(TestArgs), nameof(TestArgs.Args))]
        public void LoadPatternFromJson(string json, PatternSettings expected)
        {
            var actual = new ConfReader().LoadSettings(json);
            Assert.That(expected, Is.EqualTo(actual));
        }
    }
    internal sealed class TestArgs
    {
        public static IEnumerable<TestCaseData> Args
        {
            get
            {
                yield return new TestCaseData("{\"DrawType\": \"Polyline\", \"PenColor\": 0, \"BrushColor\": 16777042, \"BrushBkColor\": 11382189, \"Width\": 1, \"Closed\": \"true\", \"BitmapName\":\"DRO32\", \"BitmapIndex\": 47, \"Transparent\": \"true\", \"nPenStyle\": 0}", new PatternSettings("DRO32", 47));
            }
        }
    }
}