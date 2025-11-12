using Fanex.Data.Repository;

namespace ONEHub.GOTO.Core.Criteria;

public class GetAllRedirectLinkCriteria : CriteriaBase
{
    public override string GetSettingKey() => "OH_ClickStatistics_BulkCache";
    public override bool IsValid() => true;
}
