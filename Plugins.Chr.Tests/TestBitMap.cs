namespace Plugins.Tests;

[TestFixture]
internal class TestBitMap
{
    [Test]
    [TestCaseSource(typeof(TestBitMapDataSource), nameof(TestBitMapDataSource.CtorArgs))]
    public void BIT_MAP_AddPoint_ResizeField(BitMap map, int x, int y, BitMap expected)
    {
        var actual = map.Clone() as BitMap ?? throw new ArgumentException(null, nameof(map));
        actual.AddPoint(x, y);
        Assert.Multiple(() =>
        {
            Assert.That(expected.Height, Is.EqualTo(actual.Height));
            Assert.That(expected.Width, Is.EqualTo(actual.Width));
            Assert.That(expected.ByteInnerLength, Is.EqualTo(actual.ByteInnerLength));
            Assert.That(expected.OffsetX, Is.EqualTo(actual.OffsetX));
            Assert.That(expected.OffsetY, Is.EqualTo(actual.OffsetY));
        });
    }
    [Test]
    [TestCaseSource(typeof(TestBitMapDataSource), nameof(TestBitMapDataSource.LineArgs))]
    public void BIT_MAP_Line(BitMap map, int x1, int y1, int x2, int y2, BitMap expected)
    {
        var actual = map.Clone() as BitMap ?? throw new ArgumentException(null, nameof(map));
        actual.Line(x1, y1, x2, y2);
        Assert.Multiple(() =>
        {
            Assert.That(expected.Height, Is.EqualTo(actual.Height));
            Assert.That(expected.Width, Is.EqualTo(actual.Width));
            Assert.That(expected.ByteInnerLength, Is.EqualTo(actual.ByteInnerLength));
            Assert.That(expected.OffsetX, Is.EqualTo(actual.OffsetX));
            Assert.That(expected.OffsetY, Is.EqualTo(actual.OffsetY));
            CollectionAssert.AreEqual(actual.Field, expected.Field);
        });
    }
}
internal sealed class TestBitMapDataSource
{
    private readonly static BitMap empty = new(0, 0);
    #region BIT_MAP Object 0
    private readonly static BitMap step_0_0 = new(new byte[,] { { 0b00000000 } }, 0, 7, 1);
    private readonly static BitMap step_0_1 = new(new byte[2, 1]
    {
        { 0b01000000 },
        { 0b10000000 }
    }, -1, 7, 2);
    private readonly static BitMap step_0_2 = new(new byte[,]
    {
        { 0b01000000 },
        { 0b10000000 },
        { 0b10000000 }
    }, -1, 7, 2);
    private readonly static BitMap step_0_3 = new(new byte[,]
    {
        { 0b00100000 },
        { 0b01000000 },
        { 0b01000000 },
        { 0b10000000 }
    }, -2, 7, 3);
    private readonly static BitMap step_0_4 = new(new byte[,]
    {
        { 0b00100000 },
        { 0b01000000 },
        { 0b01000000 },
        { 0b10000000 },
        { 0b10000000 },
    }, -2, 7, 3);
    private readonly static BitMap step_0_5 = new(new byte[,]
    {
        { 0b00100000 },
        { 0b01000000 },
        { 0b01000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b01000000 }
    }, -2, 7, 3);
    private readonly static BitMap step_0_6 = new(new byte[,]
    {
        { 0b00010000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b01000000 },
        { 0b01000000 },
        { 0b00100000 },
        { 0b01000000 },
        { 0b10000000 },
    }, -3, 7, 4);
    private readonly static BitMap step_0_7 = new(new byte[,]
    {
        { 0b00010000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b01000000 },
        { 0b01000000 },
        { 0b00100000 },
        { 0b01000000 },
        { 0b10000000 },
        { 0b10000000 },
    }, -3, 7, 4);
    private readonly static BitMap step_0_8 = new(new byte[,]
    {
        { 0b00010000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b01000000 },
        { 0b01000000 },
        { 0b00100000 },
        { 0b01000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b01000000 },
    }, -3, 7, 4);
    private readonly static BitMap step_0_9 = new(new byte[,]
    {
        { 0b00010000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b01000000 },
        { 0b01000000 },
        { 0b00100000 },
        { 0b01000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b01111100 },
    }, -3, 7, 6);
    private readonly static BitMap step_0_10 = new(new byte[,]
    {
        { 0b00010000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b01000000 },
        { 0b01000000 },
        { 0b00100000 },
        { 0b01000000 },
        { 0b10000000 },
        { 0b10000010 },
        { 0b01111100 },
    }, -3, 7, 7);
    #endregion

    #region BIT_MAP Object 1
    private readonly static BitMap step_1_1 = new(new byte[,]
    {
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
    }, 0, 7, 1);
    private readonly static BitMap step_1_2 = new(new byte[,]
    {
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b10000000 },
        { 0b11100000 },
    }, 0, 7, 3);
    private readonly static BitMap step_1_3 = new(new byte[,]
    {
        { 0b00100000 },
        { 0b01100000 },
        { 0b10100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00111000 },
    }, -2, 7, 5);
    private readonly static BitMap step_1_4 = new(new byte[,]
    {
        { 0b00100000 },
        { 0b01110000 },
        { 0b10101000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00100000 },
        { 0b00111000 },
    }, -2, 7, 5);
    private readonly static BitMap step_1_5 = new(new byte[,]
    {
        { 0b00010000 },
        { 0b00111000 },
        { 0b01010100 },
        { 0b00010000 },
        { 0b00110000 },
        { 0b01010000 },
        { 0b10010000 },
        { 0b00010000 },
        { 0b00010000 },
        { 0b00010000 },
        { 0b00010000 },
        { 0b00010000 },
        { 0b00010000 },
        { 0b00011100 },
    }, -3, 7, 6);
    private readonly static BitMap step_1_6 = new(new byte[,]
    {
        { 0b00010000 },
        { 0b00111000 },
        { 0b01010100 },
        { 0b00010000 },
        { 0b00111000 },
        { 0b01010100 },
        { 0b10010010 },
        { 0b00010000 },
        { 0b00010000 },
        { 0b00010000 },
        { 0b00010000 },
        { 0b00010000 },
        { 0b00010000 },
        { 0b00011100 },
    }, -3, 7, 7);
    private readonly static BitMap step_1_7 = new(new byte[,]
    {
        { 0b00001000 },
        { 0b00011100 },
        { 0b00101010 },
        { 0b00001000 },
        { 0b00011100 },
        { 0b00101010 },
        { 0b01001001 },
        { 0b00011000 },
        { 0b00101000 },
        { 0b01001000 },
        { 0b10001000 },
        { 0b00001000 },
        { 0b00001000 },
        { 0b00001110 },
    }, -4, 7, 8);
    private readonly static BitMap step_1_8 = new(new byte[,]
    {
        { 0b00001000, 0b00000000 },
        { 0b00011100, 0b00000000 },
        { 0b00101010, 0b00000000 },
        { 0b00001000, 0b00000000 },
        { 0b00011100, 0b00000000 },
        { 0b00101010, 0b00000000 },
        { 0b01001001, 0b00000000 },
        { 0b00011100, 0b00000000 },
        { 0b00101010, 0b00000000 },
        { 0b01001001, 0b00000000 },
        { 0b10001000, 0b10000000 },
        { 0b00001000, 0b00000000 },
        { 0b00001000, 0b00000000 },
        { 0b00001110, 0b00000000 },
    }, -4, 7, 1);
    #endregion

    #region BIT_MAP Object 2
    private readonly static BitMap step_2_1 = new(new byte[,]
    {
        { 0b10000000 },
        { 0b01000000 },
        { 0b00100000 },
    }, -1, 1, 3);
    private readonly static BitMap step_2_2 = new(new byte[,]
    {
        { 0b10000000 },
        { 0b01000000 },
        { 0b11100000 },
    }, -1, 1, 3);
    private readonly static BitMap step_2_3 = new(new byte[,]
    {
        { 0b10100000 },
        { 0b01000000 },
        { 0b11100000 },
    }, -1, 1, 3);
    private readonly static BitMap step_2_4 = new(new byte[,]
    {
        { 0b11100000 },
        { 0b01000000 },
        { 0b11100000 },
    }, -1, 1, 3);
    private readonly static BitMap step_2_5 = new(new byte[,]
    {
        { 0b11100000 },
        { 0b11000000 },
        { 0b11100000 },
    }, -1, 1, 3);
    private readonly static BitMap step_2_6 = new(new byte[,]
    {
        { 0b11100000 },
        { 0b11100000 },
        { 0b11100000 },
    }, -1, 1, 3);
    private readonly static BitMap step_2_7 = step_2_6;
    private readonly static BitMap step_2_8 = step_2_6;
    private readonly static BitMap step_2_9 = new(new byte[,]
    {
        { 0b11111110 },
        { 0b00000000 },
        { 0b00111000 },
        { 0b00111000 },
        { 0b00111000 },
    }, -3, 3, 7);
    #endregion

    #region TestArgs
    public static IEnumerable<TestCaseData> CtorArgs
    {
        get
        {
            yield return new TestCaseData(step_0_0, -1, 6, step_0_1);
            yield return new TestCaseData(step_0_1, -1, 5, step_0_2);
            yield return new TestCaseData(step_0_2, -2, 4, step_0_3);
            yield return new TestCaseData(step_0_3, -2, 3, step_0_4);
        }
    }
    public static IEnumerable<TestCaseData> LineArgs
    {
        get
        {
            yield return new TestCaseData(empty, 0, 7, -1, 6, step_0_1);
            yield return new TestCaseData(step_0_1, -1, 6, -1, 5, step_0_2);
            yield return new TestCaseData(step_0_2, -1, 5, -2, 4, step_0_3);
            yield return new TestCaseData(step_0_3, -2, 4, -2, 3, step_0_4);
            yield return new TestCaseData(step_0_4, -2, 3, -1, 2, step_0_5);
            yield return new TestCaseData(step_0_5, -1, 2, -3, 0, step_0_6);
            yield return new TestCaseData(step_0_6, -3, 0, -3, -1, step_0_7);
            yield return new TestCaseData(step_0_7, -3, -1, -2, -2, step_0_8);
            yield return new TestCaseData(step_0_8, -2, -2, 2, -2, step_0_9);
            yield return new TestCaseData(step_0_9, 2, -2, 3, -1, step_0_10);

            yield return new TestCaseData(empty, 0, 7, 0, -6, step_1_1);
            yield return new TestCaseData(step_1_1, 0, -6, 2, -6, step_1_2);
            yield return new TestCaseData(step_1_2, -2, 5, 0, 7, step_1_3);
            yield return new TestCaseData(step_1_3, 0, 7, 2, 5, step_1_4);
            yield return new TestCaseData(step_1_4, 0, 4, -3, 1, step_1_5);
            yield return new TestCaseData(step_1_5, 0, 4, 3, 1, step_1_6);
            yield return new TestCaseData(step_1_6, 0, 1, -4, -3, step_1_7);
            yield return new TestCaseData(step_1_7, 0, 1, 4, -3, step_1_8);

            yield return new TestCaseData(empty, -1, 1, 1, -1, step_2_1);
            yield return new TestCaseData(step_2_1, 1, -1, -1, -1, step_2_2);
            yield return new TestCaseData(step_2_2, -1, -1, 1, 1, step_2_3);
            yield return new TestCaseData(step_2_3, 1, 1, -1, 1, step_2_4);
            yield return new TestCaseData(step_2_4, -1, 1, -1, -1, step_2_5);
            yield return new TestCaseData(step_2_5, 1, 1, 1, -1, step_2_6);
            yield return new TestCaseData(step_2_6, -1, 0, 1, 0, step_2_7);
            yield return new TestCaseData(step_2_7, 0, 1, 0, -1, step_2_8);
            yield return new TestCaseData(step_2_8, -3, 3, 3, 3, step_2_9);
        }
    }
    #endregion
}
