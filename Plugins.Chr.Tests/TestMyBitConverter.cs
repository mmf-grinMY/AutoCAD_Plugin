namespace Plugins.Tests;
public class TestMyBitConverter
{
    [Test]
    [TestCase(200, 6)]
    public void Bit(int limit, int bitNumber)
    {
        for (byte i = 0; i < limit; i++)
        {
            byte expected;
            try { expected = Convert.ToByte(Convert.ToString(i, 2)[^(bitNumber+1)] - 48); }
            catch { expected = 0; }
            Assert.That(MyBitConverter.Bit(i, bitNumber), Is.EqualTo(expected));
        }
    }
    [Test]
    [TestCase(256, 6)]
    public void BitReset(int limit, int bitNumber)
    {
        for (int i = 0; i < limit; i++) 
        {
            byte expected;
            var bits = Convert.ToString(i, 2).ToCharArray();
            if (bits.Length >= ++bitNumber && bits[^bitNumber] == '1')
            {
                bits[^bitNumber] = '0';
            }
            expected = Convert.ToByte(new string(bits), 2);
            var actual = MyBitConverter.BitReset((byte)i, --bitNumber);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}