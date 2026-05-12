using NUnit.Framework;
using TrainAI.Core;
using TrainAI.Sentis;

namespace TrainAI.EditMode.Tests
{
    public class VietnameseTokenizerTests
    {
        const string SampleMeta = @"{
            ""max_len"": 8,
            ""pad_id"": 0,
            ""unk_id"": 1,
            ""vocab"": {
                ""<pad>"": 0,
                ""<unk>"": 1,
                ""hôm"": 2,
                ""nay"": 3,
                ""hôm nay"": 4,
                ""có"": 5,
                ""gì"": 6
            }
        }";

        [Test]
        public void Encode_PrefersLongestMultiWordMatch()
        {
            ITokenizer tk = new VietnameseTokenizer(SampleMeta);
            int[] ids = tk.Encode("hôm nay có gì");
            Assert.AreEqual(4, ids[0], "should match 'hôm nay' as single token id 4");
            Assert.AreEqual(5, ids[1]);
            Assert.AreEqual(6, ids[2]);
        }

        [Test]
        public void Encode_UnknownTokens_MapToUnk()
        {
            ITokenizer tk = new VietnameseTokenizer(SampleMeta);
            int[] ids = tk.Encode("xyz");
            Assert.AreEqual(1, ids[0], "should map unknown to unk_id=1");
        }

        [Test]
        public void Encode_PadsToMaxLen()
        {
            ITokenizer tk = new VietnameseTokenizer(SampleMeta);
            int[] ids = tk.Encode("hôm");
            Assert.AreEqual(8, ids.Length);
            Assert.AreEqual(2, ids[0]);
            Assert.AreEqual(0, ids[1], "remaining slots should be pad_id=0");
        }
    }
}
