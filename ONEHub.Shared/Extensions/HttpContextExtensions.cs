using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace ONEHub.Shared.Extensions;

public static class HttpContextExtensions
{
    private const string ChromeBrowser = "Chrome";
    private const string SafariBrowser = "Safari";
    private const string EdgeBrowser = "Edge";
    private const string OperaBrowser = "Opera";
    private const string FirefoxBrowser = "Firefox";
    private const string InternetExplorerBrowser = "Internet Explorer";

    private static readonly IReadOnlyDictionary<string, string> _operatingSystemMapping = new Dictionary<string, string>
        {
                { "Windows NT 6.1", "Windows 7" },
                { "Windows NT 6.0", "Windows Vista" },
                { "Windows NT 5.1", "Windows XP" },
                { "Windows NT 5.0", "Windows 2000" },
                { "Windows NT 4.1", "Windows 98" },
                { "iPhone", "iOS" },
                { "iPad", "iOS" },
                { "iPod", "iOS" },
                { "Mac OS", "Mac OS" },
                { "BlackBerry", "BlackBerry" },
                { "Android", "Android" },
                { "Linux", "Linux" },
                { "Ubuntu", "Ubuntu" }
        };

    private static readonly IReadOnlyDictionary<string, string> _deviceMapping = new Dictionary<string, string>
        {
            { "iphone", "iPhone" },
            { "ipad", "iPad" },
            { "ipod", "iPod" },
            { "blackberry", "BlackBerry" },
            { "htc", "HTC" },
            { "motorola", "Motorola" },
            { "nokia", "Nokia" }
        };

    public static string GetClientIpAddress(this HttpContext httpContext)
    {
        var realIp = httpContext.Request.Headers["x-actual-ip"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }
        else
        {
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }

    public static string GetUserAgent(this HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        return userAgent ?? string.Empty;
    }

    public static string GetLanguage(this HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers["Accept-Language"].ToString();
        return userAgent ?? string.Empty;
    }

    public static Uri? GetURLReferrer(this HttpContext httpContext)
    {
        var urlReferrer = httpContext.Request.Headers["Referer"].ToString();
        if (string.IsNullOrEmpty(urlReferrer))
        {
            urlReferrer = httpContext.Request.Query["ref"].ToString();
        }

        if (string.IsNullOrEmpty(urlReferrer))
        {
            return null;
        }

        if (Uri.TryCreate(urlReferrer, UriKind.Absolute, out var uri))
        {
            return uri;
        }

        return null;
    }

    public static string GetDomainReferrer(this HttpContext httpContext)
    {
        var urlReferrer = GetURLReferrer(httpContext);
        if (urlReferrer != null && !string.IsNullOrEmpty(urlReferrer.Host))
        {
            return urlReferrer.Host;
        }

        return string.Empty;
    }

    public static string GetBrowserName(this HttpContext httpContext)
    {
        var userAgent = GetUserAgent(httpContext);
        if (string.IsNullOrEmpty(userAgent))
        {
            return string.Empty;
        }

        userAgent = userAgent.ToLowerInvariant();

        // Define browser patterns in order of specificity
        var browserPatterns = new[]
        {
                (EdgeBrowser, new[] { "edg/", "edge/" }),
                (OperaBrowser, new[] { "opr/", "opera" }),
                (ChromeBrowser, new[] { "chrome" }),
                (SafariBrowser, new[] { "safari" }),
                (FirefoxBrowser, new[] { "firefox" }),
                (InternetExplorerBrowser, new[] { "msie", "trident" })
            };

        foreach (var (browserName, patterns) in browserPatterns)
        {
            if (patterns.ToList().Exists(pattern => userAgent.Contains(pattern)))
            {
                // Special handling for Chrome to avoid false positives
                if (browserName == ChromeBrowser && (userAgent.Contains("edg/") || userAgent.Contains("opr/")))
                {
                    continue;
                }
                // Special handling for Safari to avoid Chrome false positives
                if (browserName == SafariBrowser && userAgent.Contains("chrome"))
                {
                    continue;
                }
                return browserName;
            }
        }

        return "Other";
    }

    public static string GetBrowserVersion(this HttpContext httpContext)
    {
        var userAgent = GetUserAgent(httpContext);
        if (string.IsNullOrEmpty(userAgent))
        {
            return string.Empty;
        }

        userAgent = userAgent.ToLowerInvariant();

        // Define browser version patterns in order of specificity
        var browserVersionPatterns = new[]
        {
                (EdgeBrowser, new[] { @"edg/([\d\.]+)", @"edge/([\d\.]+)" }),
                (OperaBrowser, new[] { @"opr/([\d\.]+)", @"opera/([\d\.]+)" }),
                (ChromeBrowser, new[] { @"chrome/([\d\.]+)" }),
                (SafariBrowser, new[] { @"version/([\d\.]+)" }),
                (FirefoxBrowser, new[] { @"firefox/([\d\.]+)" }),
                (InternetExplorerBrowser, new[] { @"msie ([\d\.]+)", @"rv:([\d\.]+)" })
            };

        foreach (var (browserName, patterns) in browserVersionPatterns)
        {
            var version = GetVersionForBrowser(userAgent, browserName, patterns);
            if (!string.IsNullOrEmpty(version))
            {
                return version;
            }
        }

        return string.Empty;
    }

    private static string GetVersionForBrowser(string userAgent, string browserName, string[] patterns)
    {
        const int timeOutSecond = 2;
        foreach (var pattern in patterns)
        {
            
            var match = Regex.Match(userAgent, pattern, RegexOptions.None, TimeSpan.FromSeconds(timeOutSecond));
            if (match.Success && IsValidBrowserMatch(userAgent, browserName, pattern))
            {
                return match.Groups[1].Value;
            }
        }
        return string.Empty;
    }

    private static bool IsValidBrowserMatch(string userAgent, string browserName, string pattern)
    {
        // Special handling for Chrome to avoid false positives
        if (browserName == ChromeBrowser && (userAgent.Contains("edg/") || userAgent.Contains("opr/")))
        {
            return false;
        }
        // Special handling for Safari to avoid Chrome false positives
        if (browserName == SafariBrowser && userAgent.Contains("chrome"))
        {
            return false;
        }
        // Special handling for IE Trident
        if (browserName == InternetExplorerBrowser && pattern == @"rv:([\d\.]+)" && !userAgent.Contains("trident"))
        {
            return false;
        }
        return true;
    }

    public static string GetOsName(this HttpContext httpContext)
    {
        var userAgent = GetUserAgent(httpContext);
        if (string.IsNullOrEmpty(userAgent))
        {
            return string.Empty;
        }

        var osKey = _operatingSystemMapping.Keys.FirstOrDefault(key => userAgent.Contains(key));

        return string.IsNullOrEmpty(osKey) ? string.Empty : _operatingSystemMapping[osKey];
    }

    public static bool IsMobile(this HttpContext httpContext)
    {
        var request = httpContext.Request;
        if (request == null)
        {
            return false;
        }

        // Try to get the User-Agent header
        var userAgent = GetUserAgent(httpContext);
        if (string.IsNullOrEmpty(userAgent))
        {
            return false;
        }

        // Use regex patterns to detect mobile devices
        // These patterns are based on common mobile user agent substrings
        const int ValidLength = 4;
        if (userAgent.Length < ValidLength)
        {
            return false;
        }

        var b = new Regex(
            @"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|" +
            @"ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|" +
            @"phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|" +
            @"windows ce|xda|xiino",
            RegexOptions.IgnoreCase | RegexOptions.Multiline, TimeSpan.FromSeconds(1));
        var v = new Regex(
            @"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|" +
            @"amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|" +
            @"bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|" +
            @"da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|" +
            @"esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|" +
            @"haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|" +
            @"i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|" +
            @"jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|" +
            @"libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|" +
            @"mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|" +
            @"n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|" +
            @"owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|" +
            @"psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|" +
            @"sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|" +
            @"sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|" +
            @"ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|" +
            @"utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|" +
            @"w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-",
            RegexOptions.IgnoreCase | RegexOptions.Multiline, TimeSpan.FromSeconds(1));

        // Check for mobile device patterns
        if (b.IsMatch(userAgent) || v.IsMatch(userAgent.Substring(0, ValidLength)))
        {
            return true;
        }

        // Additional check: common mobile keywords
        var mobileKeywords = new[] { "mobile", "android", "iphone", "ipad", "ipod", "opera mini", "iemobile", "blackberry", "webos", "windows phone" };
        var uaLower = userAgent.ToLowerInvariant();
        if (mobileKeywords.ToList().Exists(keyword => uaLower.Contains(keyword)))
        {
            return true;
        }

        return false;
    }

    public static string GetDeviceName(this HttpContext httpContext)
    {
        var userAgent = GetUserAgent(httpContext);
        if (string.IsNullOrEmpty(userAgent))
        {
            return @"Computer/Laptop";
        }

        var deviceKey = _deviceMapping.Keys.FirstOrDefault(key =>
            userAgent.ToLower(CultureInfo.InvariantCulture).Contains(key, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(deviceKey))
        {
            return IsMobile(httpContext) ? "Mobile" : @"Computer/Laptop";
        }

        return _deviceMapping[deviceKey];
    }
}
