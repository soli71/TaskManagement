using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;

namespace TaskManagementMvc.TagHelpers;

// Usage: <corp-select asp-for="CompanyId" asp-items="Model.Companies" placeholder="انتخاب شرکت" dense icon="bi-building"></corp-select>
[HtmlTargetElement("corp-select", Attributes = ForAttributeName)]
public class CorporateSelectTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";
    private const string ItemsAttributeName = "asp-items";
    private const string PlaceholderAttributeName = "placeholder";
    private const string IconAttributeName = "icon";
    private const string DenseAttributeName = "dense";
    private const string RequiredAttributeName = "required";

    [HtmlAttributeName(ForAttributeName)] public ModelExpression For { get; set; } = default!;
    [HtmlAttributeName(ItemsAttributeName)] public IEnumerable<SelectListItem>? Items { get; set; }
    [HtmlAttributeName(PlaceholderAttributeName)] public string? Placeholder { get; set; }
    [HtmlAttributeName(IconAttributeName)] public string? Icon { get; set; }
    [HtmlAttributeName(DenseAttributeName)] public bool Dense { get; set; }
    [HtmlAttributeName(RequiredAttributeName)] public bool Required { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div"; // wrapper
        var wrapperClass = new StringBuilder("corp-field mb-3");
        if (Dense) wrapperClass.Append(" corp-dense");
        output.Attributes.SetAttribute("class", wrapperClass.ToString());

        var id = For.Name.Replace('.', '_');
        var labelText = For.Metadata.DisplayName ?? For.Name;

        var sb = new StringBuilder();
    var labelHtml = new StringBuilder();
    labelHtml.Append($"<label for=\"{id}\" class=\"form-label corp-label\">");
    labelHtml.Append(labelText);
    if (Required) labelHtml.Append(" <span class=\"text-danger\">*</span>");
    labelHtml.Append("</label>");
    sb.AppendLine(labelHtml.ToString());

        if (!string.IsNullOrWhiteSpace(Icon))
        {
            sb.AppendLine("<div class=\"corp-input-group\">");
            sb.AppendLine($"<span class=\"corp-input-icon\"><i class=\"bi {Icon}\"></i></span>");
        }
        else
        {
            sb.AppendLine("<div class=\"corp-input-raw\">");
        }

        var selectClasses = new StringBuilder("form-select corp-select");
        if (Dense) selectClasses.Append(" corp-select-dense");

    sb.Append($"<select id=\"{id}\" name=\"{For.Name}\" class=\"{selectClasses}\"");
    if (Required) sb.Append(" required");
    sb.AppendLine(">\n");
        var currentValue = For.Model?.ToString();
        if (!string.IsNullOrWhiteSpace(Placeholder))
        {
            bool selected = string.IsNullOrWhiteSpace(currentValue);
            sb.AppendLine($"<option value=\"\"{(selected ? " selected" : string.Empty)} disabled>{System.Net.WebUtility.HtmlEncode(Placeholder)}</option>");
        }
        if (Items != null)
        {
            foreach (var item in Items)
            {
                var sel = item.Value == currentValue ? " selected" : string.Empty;
                sb.AppendLine($"<option value=\"{System.Net.WebUtility.HtmlEncode(item.Value)}\"{sel}>{System.Net.WebUtility.HtmlEncode(item.Text)}</option>");
            }
        }
        if (Items == null)
        {
            // Allow manual <option> tags inside the corp-select element
            var child = output.GetChildContentAsync().Result?.GetContent();
            if (!string.IsNullOrWhiteSpace(child))
            {
                sb.AppendLine(child);
            }
        }
        sb.AppendLine("</select>");
        sb.AppendLine("</div>"); // input/icon wrapper
        sb.AppendLine($"<span class=\"text-danger corp-validation\" data-valmsg-for=\"{For.Name}\" data-valmsg-replace=\"true\"></span>");

        output.Content.SetHtmlContent(sb.ToString());
    }
}
