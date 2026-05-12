using NUnit.Framework;
using UnityEngine;
using TrainAI.SO.Base;
using TrainAI.SO.Concrete;

namespace TrainAI.EditMode.Tests
{
    public class EndingRuleTests
    {
        [Test]
        public void XuatSac_RequiresBothHigh()
        {
            var rule = ScriptableObject.CreateInstance<MilitaryAcademyEndingSO>();
            var st = ScriptableObject.CreateInstance<PlayerStateRSO>();
            st.hocTap = 432; st.renLuyen = 90;
            Assert.AreEqual(EndingGrade.XuatSac, rule.Evaluate(st));
        }

        [Test]
        public void Tot_MidRange()
        {
            var rule = ScriptableObject.CreateInstance<MilitaryAcademyEndingSO>();
            var st = ScriptableObject.CreateInstance<PlayerStateRSO>();
            st.hocTap = 300; st.renLuyen = 70;
            Assert.AreEqual(EndingGrade.Tot, rule.Evaluate(st));
        }

        [Test]
        public void TrungBinh_BelowThreshold()
        {
            var rule = ScriptableObject.CreateInstance<MilitaryAcademyEndingSO>();
            var st = ScriptableObject.CreateInstance<PlayerStateRSO>();
            st.hocTap = 100; st.renLuyen = 40;
            Assert.AreEqual(EndingGrade.TrungBinh, rule.Evaluate(st));
        }
    }
}
