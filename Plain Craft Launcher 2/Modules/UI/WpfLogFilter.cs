using System.Text;

namespace PCL;

/// <summary>日志敏感信息打码桥接。KSP 改造后：原 MC 登录态/令牌打码逻辑已剥离，此处提供通用的令牌打码。</summary>
public sealed class WpfLogFilter : ILogFilter
{
    public string FilterUserName(string raw, char filterChar)
    {
        return raw;
    }

    public string FilterAccessToken(string raw, char filterChar)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        var mask = new StringBuilder(raw.Length);
        foreach (var c in raw)
            mask.Append(c == ' ' ? ' ' : filterChar);
        return mask.ToString();
    }
}

/// <summary>区域限制功能判定桥接。KSP 改造后不做区域限制，全部放行。</summary>
public sealed class WpfRestrictedFeatures : IRestrictedFeatures
{
    public bool IsRestricted => false;

    public bool Check(string feature) => true;

    public void ThrowIfRestricted(string feature) { }
}
