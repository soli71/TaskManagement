using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;

namespace TaskManagementMvc.TagHelpers;

// Usage:
// <corp-table headers="نام,ایمیل,نقش" striped hover small responsive caption="کاربران">
//   <tr><td>...</td><td>...</td><td>...</td></tr>
// </corp-table>
[HtmlTargetElement("corp-table")]
public class CorporateTableTagHelper : TagHelper
{
    [HtmlAttributeName("headers")] public string? Headers { get; set; }
    [HtmlAttributeName("striped")] public bool Striped { get; set; }
    [HtmlAttributeName("hover")] public bool Hover { get; set; }
    [HtmlAttributeName("small")] public bool Small { get; set; }
    [HtmlAttributeName("responsive")] public bool Responsive { get; set; } = true;
    [HtmlAttributeName("caption")] public string? Caption { get; set; }
    [HtmlAttributeName("dense")] public bool Dense { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = Responsive ? "div" : "table";
        var tableClasses = new StringBuilder("table corp-table");
        if (Striped) tableClasses.Append(" table-striped");
        if (Hover) tableClasses.Append(" table-hover");
        if (Small || Dense) tableClasses.Append(" table-sm");
        if (Dense) tableClasses.Append(" corp-table-dense");

        var inner = new StringBuilder();
        inner.Append('<').Append("table class=\"").Append(tableClasses).Append("\">");
        if (!string.IsNullOrWhiteSpace(Caption))
            inner.Append("<caption class=\"corp-table-caption\">").Append(Caption).Append("</caption>");
        if (!string.IsNullOrWhiteSpace(Headers))
        {
            inner.Append("<thead><tr>");
            foreach (var h in Headers.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
            {
                inner.Append("<th scope=\"col\">").Append(System.Net.WebUtility.HtmlEncode(h.Trim())).Append("</th>");
            }
            inner.Append("</tr></thead>");
        }
        inner.Append("<tbody>").Append(output.GetChildContentAsync().Result.GetContent()).Append("</tbody></table>");
        output.Content.SetHtmlContent(inner.ToString());
        if (Responsive)
        {
            output.Attributes.SetAttribute("class", "table-responsive corp-table-wrapper");
        }
    }
}
