using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;

namespace TaskManagementMvc.TagHelpers
{
    // Usage examples:
    // <corp-btn variant="primary" icon="bi-check2" type="submit">ثبت</corp-btn>
    // <corp-btn variant="outline" icon="bi-x" asp-action="Index">انصراف</corp-btn>
    // Attributes:
    // variant: primary | secondary | outline | danger | success
    // icon: bootstrap icon class (without leading dot)
    // dense: compact size
    // asp-action / asp-controller / asp-route-id (optional) => renders anchor instead of button
    [HtmlTargetElement("corp-btn")]
    public class CorporateButtonTagHelper : TagHelper
    {
        private readonly IUrlHelperFactory _urlHelperFactory;
        public CorporateButtonTagHelper(IUrlHelperFactory urlHelperFactory) => _urlHelperFactory = urlHelperFactory;

    [HtmlAttributeNotBound]
    [ViewContext]
    public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { get; set; } = default!;

        [HtmlAttributeName("variant")] public string? Variant { get; set; }
        [HtmlAttributeName("icon")] public string? Icon { get; set; }
        [HtmlAttributeName("type")] public string? ButtonType { get; set; }
        [HtmlAttributeName("dense")] public bool Dense { get; set; }

        // Optional routing (mirror anchor tag helper minimal subset)
        [HtmlAttributeName("asp-action")] public string? AspAction { get; set; }
        [HtmlAttributeName("asp-controller")] public string? AspController { get; set; }
        [HtmlAttributeName("asp-route-id")] public string? AspRouteId { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            bool isLink = !string.IsNullOrWhiteSpace(AspAction) || !string.IsNullOrWhiteSpace(AspController);
            var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);

            string tag = isLink ? "a" : "button";
            output.TagName = tag;

            var classes = new StringBuilder("btn corp-btn");
            classes.Append(' ').Append(ResolveBootstrapVariant(Variant));
            if (Dense) classes.Append(" corp-btn-dense");

            output.Attributes.SetAttribute("class", classes.ToString());

            if (isLink)
            {
                string action = AspAction ?? ViewContext.RouteData.Values["action"]?.ToString() ?? "Index";
                string controller = AspController ?? ViewContext.RouteData.Values["controller"]?.ToString() ?? string.Empty;
                var routeValues = new { id = AspRouteId };
                string href = urlHelper.Action(action, controller, string.IsNullOrWhiteSpace(AspRouteId) ? null : routeValues) ?? "#";
                output.Attributes.SetAttribute("href", href);
                output.Attributes.SetAttribute("role", "button");
            }
            else
            {
                output.Attributes.SetAttribute("type", string.IsNullOrWhiteSpace(ButtonType) ? "submit" : ButtonType!);
            }

            var content = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(Icon))
            {
                content.Append('<').Append("i class=\"bi ").Append(Icon).Append("\"></i> ");
            }
            output.PreContent.SetHtmlContent(content.ToString());
        }

        private static string ResolveBootstrapVariant(string? variant) => variant?.ToLowerInvariant() switch
        {
            "secondary" => "btn-secondary",
            "outline" => "btn-outline-secondary",
            "danger" => "btn-danger",
            "success" => "btn-success",
            _ => "btn-primary"
        };
    }
}
