using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TaskManagementMvc.TagHelpers;

// Usage: <corp-label asp-for="Name" required-indicator></corp-label>
[HtmlTargetElement("corp-label", Attributes = ForAttributeName)]
public class CorporateLabelTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";
    private const string DenseAttributeName = "dense";
    private const string RequiredIndicatorAttributeName = "required-indicator";

    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression For { get; set; } = default!;

    [HtmlAttributeName(DenseAttributeName)] public bool Dense { get; set; }
    [HtmlAttributeName(RequiredIndicatorAttributeName)] public bool ShowRequiredIndicator { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "label";
        var id = For.Name.Replace('.', '_');
        output.Attributes.SetAttribute("for", id);
        var cls = "form-label corp-label" + (Dense ? " corp-label-dense" : string.Empty);
        output.Attributes.SetAttribute("class", cls);

        var labelText = For.Metadata.DisplayName ?? For.Name;
        if (ShowRequiredIndicator || For.Metadata.IsRequired)
        {
            labelText += " <span class=\"corp-label-required\">*</span>";
        }
        output.Content.SetHtmlContent(labelText);
    }
}
