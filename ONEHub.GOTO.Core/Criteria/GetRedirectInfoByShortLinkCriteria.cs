using Fanex.Data.Repository;

namespace ONEHub.GOTO.Core.Criteria;

public class GetRedirectInfoByShortLinkCriteria : CriteriaBase
{
    public string LinkCode { get; set; } = string.Empty;

    public override string GetSettingKey() => "OH_ClickStatistics_RedirectPage_Sel";
    public override bool IsValid() => !string.IsNullOrWhiteSpace(LinkCode);
}
