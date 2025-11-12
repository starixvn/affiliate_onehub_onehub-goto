using Fanex.Data.Repository;

namespace ONEHub.GOTO.Core.Criteria;

public class GetClickStatisticCriteria : CriteriaBase
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public override string GetSettingKey() => "OH_ClickStatisticsPatterns_Sel";
    public override bool IsValid() => ToDate >= FromDate;
}
