using UnityEngine;
using TrainAI.SO.Base;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Rule_Ending_Academy", menuName = "TrainAI/Rule/Ending Military Academy")]
    public class MilitaryAcademyEndingSO : EndingRuleSO
    {
        public override EndingGrade Evaluate(PlayerStateRSO s)
        {
            if (s.hocTap >= 432 && s.renLuyen >= 90) return EndingGrade.XuatSac;
            if (s.hocTap >= 288 && s.renLuyen >= 60) return EndingGrade.Tot;
            return EndingGrade.TrungBinh;
        }
    }
}
