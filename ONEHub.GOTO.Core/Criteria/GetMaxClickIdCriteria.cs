using Fanex.Data.Repository;

namespace ONEHub.GOTO.Core.Criteria;

public class GetMaxClickIdCriteria : CriteriaBase
{
    public override string GetSettingKey() => "OH_ClickStatistics_MaxClickId_Sel";
    public override bool IsValid() => true;
}
