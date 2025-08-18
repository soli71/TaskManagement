using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;

namespace TaskManagementMvc.TagHelpers
{
    // Usage: <corp-input asp-for="Model.Property" placeholder="..." icon="bi-person" />
    [HtmlTargetElement("corp-input", Attributes = ForAttributeName)]
    public class CorporateInputTagHelper : TagHelper
    {
        private const string ForAttributeName = "asp-for";
        private const string IconAttributeName = "icon";
        private const string PlaceholderAttributeName = "placeholder";
        private const string TypeAttributeName = "type";
        private const string ReadOnlyAttributeName = "readonly";
        private const string RequiredAttributeName = "required";
        private const string DenseAttributeName = "dense";

        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; } = default!;

        [HtmlAttributeName(IconAttributeName)]
        public string? Icon { get; set; }

        [HtmlAttributeName(PlaceholderAttributeName)]
        public string? Placeholder { get; set; }

        [HtmlAttributeName(TypeAttributeName)]
        public string? InputType { get; set; }

        // When present makes the input compact / minimal
        [HtmlAttributeName(DenseAttributeName)]
        public bool Dense { get; set; }

        // Whether the underlying input should be readonly
        [HtmlAttributeName(ReadOnlyAttributeName)]
        public bool ReadOnly { get; set; }

        // Whether the input is required (adds required attribute and asterisk on label)
        [HtmlAttributeName(RequiredAttributeName)]
        public bool Required { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div"; // wrapper
            var wrapperClass = new StringBuilder("corp-field mb-3");
            if (Dense) wrapperClass.Append(" corp-dense");
            output.Attributes.Add("class", wrapperClass.ToString());

            var inputId = For.Name.Replace('.', '_');
            var label = For.Metadata.DisplayName ?? For.Name;
            var type = string.IsNullOrWhiteSpace(InputType) ? (For.Metadata.UnderlyingOrModelType == typeof(DateTime) ? "date" : "text") : InputType;

            var sb = new StringBuilder();
            // Label
            var labelHtml = new StringBuilder();
            labelHtml.Append($"<label for=\"{inputId}\" class=\"form-label corp-label\">");
            labelHtml.Append(label);
            if (Required)
            {
                labelHtml.Append(" <span class=\"text-danger\">*</span>");
            }
            labelHtml.Append("</label>");
            sb.AppendLine(labelHtml.ToString());

            // Input group wrapper if icon
            if (!string.IsNullOrWhiteSpace(Icon))
            {
                sb.AppendLine("<div class=\"corp-input-group\">");
                sb.AppendLine($"<span class=\"corp-input-icon\"><i class=\"bi {Icon}\"></i></span>");
            }
            else
            {
                sb.AppendLine("<div class=\"corp-input-raw\">");
            }

            var valueAttr = For.Model switch
            {
                DateTime dt => dt == default ? string.Empty : dt.ToString("yyyy-MM-dd"),
                DateTimeOffset dto => dto == default ? string.Empty : dto.ToString("yyyy-MM-dd"),
                _ => For.Model?.ToString() ?? string.Empty
            };

            var inputClasses = new StringBuilder("form-control corp-input");
            if (Dense) inputClasses.Append(" corp-input-dense");
            sb.Append("<input");
            sb.Append($" type=\"{type}\"");
            sb.Append($" class=\"{inputClasses}\"");
            sb.Append($" id=\"{inputId}\"");
            sb.Append($" name=\"{For.Name}\"");
            sb.Append($" value=\"{System.Net.WebUtility.HtmlEncode(valueAttr)}\"");
            sb.Append($" placeholder=\"{System.Net.WebUtility.HtmlEncode(Placeholder ?? label)}\"");
            if (ReadOnly) sb.Append(" readonly");
            if (Required) sb.Append(" required");
            sb.AppendLine(" />");
            sb.AppendLine("</div>"); // close input/icon wrapper

            // Validation span
            sb.AppendLine($"<span class=\"text-danger corp-validation\" data-valmsg-for=\"{For.Name}\" data-valmsg-replace=\"true\"></span>");

            output.Content.SetHtmlContent(sb.ToString());
        }
    }
}