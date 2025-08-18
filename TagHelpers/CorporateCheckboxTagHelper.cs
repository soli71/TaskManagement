using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;

namespace TaskManagementMvc.TagHelpers;

// Usage: <corp-check asp-for="IsActive" dense></corp-check>
[HtmlTargetElement("corp-check", Attributes = ForAttributeName)]
public class CorporateCheckboxTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";
    private const string DenseAttributeName = "dense";
    private const string LabelAfterAttributeName = "label-after"; // default true

    [HtmlAttributeName(ForAttributeName)] public ModelExpression For { get; set; } = default!;
    [HtmlAttributeName(DenseAttributeName)] public bool Dense { get; set; }
    [HtmlAttributeName(LabelAfterAttributeName)] public bool LabelAfter { get; set; } = true;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.Attributes.SetAttribute("class", "form-check corp-check" + (Dense ? " corp-check-dense" : string.Empty));

        var id = For.Name.Replace('.', '_');
        var labelText = For.Metadata.DisplayName ?? For.Name;
        bool isChecked = false;
        if (For.Model is bool b) isChecked = b;
        else if (For.Model is bool?) isChecked = ((bool?)For.Model) ?? false;

        var sb = new StringBuilder();
        // Hidden false value (matches built-in checkbox helper pattern)
        sb.AppendLine($"<input name=\"{For.Name}\" type=\"hidden\" value=\"false\" />");

        var inputTag = $"<input type=\"checkbox\" class=\"form-check-input corp-check-input\" id=\"{id}\" name=\"{For.Name}\" value=\"true\"{(isChecked ? " checked" : string.Empty)} />";
        var labelTag = $"<label class=\"form-check-label corp-check-label\" for=\"{id}\">{labelText}</label>";
        if (LabelAfter)
            sb.AppendLine(inputTag + labelTag);
        else
            sb.AppendLine(labelTag + inputTag);

        sb.AppendLine($"<span class=\"text-danger corp-validation\" data-valmsg-for=\"{For.Name}\" data-valmsg-replace=\"true\"></span>");
        output.Content.SetHtmlContent(sb.ToString());
    }
}
