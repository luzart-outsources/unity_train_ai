using NUnit.Framework;
using TrainAI.Core;
using TrainAI.Sentis;

namespace TrainAI.EditMode.Tests
{
    public class ResponseFillerTests
    {
        const string SampleResponses = @"{
            ""_README"": ""sample"",
            ""HOI_LICH"": [""Hom nay co {scheduled_today}.""],
            ""TAM_BIET"": [""Chao {playerName}.""]
        }";

        [Test]
        public void Fill_ReplacesKnownTags()
        {
            IResponseFiller filler = new ResponseFiller(SampleResponses);
            var s = filler.Fill(IntentId.HOI_LICH, k => k == "scheduled_today" ? "tap the duc" : null);
            Assert.AreEqual("Hom nay co tap the duc.", s);
        }

        [Test]
        public void Fill_UnknownTag_FallsBackToBrackets()
        {
            IResponseFiller filler = new ResponseFiller(SampleResponses);
            var s = filler.Fill(IntentId.TAM_BIET, _ => null);
            Assert.AreEqual("Chao [playerName].", s);
        }

        [Test]
        public void Fill_IntentNotInPool_ReturnsEllipsis()
        {
            IResponseFiller filler = new ResponseFiller(SampleResponses);
            Assert.AreEqual("...", filler.Fill(IntentId.HOI_KIEN_THUC, _ => null));
        }
    }
}
