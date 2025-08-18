using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;

namespace TaskManagementMvc.TagHelpers;

// Usage: <corp-badge variant="success" pill icon="bi-check2">موفق</corp-badge>
[HtmlTargetElement("corp-badge")]
public class CorporateBadgeTagHelper : TagHelper
{
    [HtmlAttributeName("variant")] public string? Variant { get; set; }
    [HtmlAttributeName("pill")] public bool Pill { get; set; }
    [HtmlAttributeName("icon")] public string? Icon { get; set; }
    [HtmlAttributeName("dense")] public bool Dense { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        var cls = new StringBuilder("badge corp-badge");
        cls.Append(' ').Append(ResolveVariant(Variant));
        if (Pill) cls.Append(" rounded-pill");
        if (Dense) cls.Append(" corp-badge-dense");
        output.Attributes.SetAttribute("class", cls.ToString());

        if (!string.IsNullOrWhiteSpace(Icon))
        {
            output.PreContent.SetHtmlContent($"<i class=\"bi {Icon}\"></i> ");
        }
    }

    private static string ResolveVariant(string? v) => (v ?? "primary").ToLowerInvariant() switch
    {
        "secondary" => "text-bg-secondary",
        "success" => "text-bg-success",
        "danger" => "text-bg-danger",
        "warning" => "text-bg-warning",
        "info" => "text-bg-info",
        _ => "text-bg-primary"
    };
}
