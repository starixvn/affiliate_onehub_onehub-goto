namespace ONEHub.Shared.Models;

public class DomainSettings
{
    public string MainMemberSite { get; set; } = string.Empty;

    public string API { get; set; } = string.Empty;

    public string MediaSite { get; set; } = string.Empty;

    public string TermsConditions { get; set; } = string.Empty;

    public string ContentSecurityPolicy { get; set; } = string.Empty;

    public string MemberSiteUM { get; set; } = string.Empty;

    public bool EnableCheckMemberSiteUM { get; set; }

    public RedirectSettings Redirect { get; set; } = new RedirectSettings();
}

public class RedirectSettings
{
    public bool RedirectToRootDomain { get; set; }

    public string SpecificDomain { get; set; } = string.Empty;

    public string DefaultPath { get; set; } = string.Empty;

    public string SuceessPath { get; set; } = string.Empty;
}
