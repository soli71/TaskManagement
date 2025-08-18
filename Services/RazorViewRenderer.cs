using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ModelBinding; // for EmptyModelMetadataProvider
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using System.Threading.Tasks;

namespace TaskManagementMvc.Services
{
    public interface IRazorViewRenderer
    {
        Task<string> RenderViewToStringAsync(Controller? controller, string viewName, object model);
    }

    public class RazorViewRenderer : IRazorViewRenderer
    {
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public RazorViewRenderer(ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderViewToStringAsync(Controller? controller, string viewName, object model)
        {
            // Build an ActionContext even if no controller provided (background jobs)
            ActionContext actionContext;
            if (controller != null)
            {
                actionContext = new ActionContext(controller.HttpContext, controller.RouteData, controller.ControllerContext.ActionDescriptor);
            }
            else
            {
                var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
                actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
            }

            using var sw = new StringWriter();

            // Build candidate paths when a simple name is supplied
            List<string> candidatePaths = new();
            if (viewName.StartsWith("~") || viewName.StartsWith("/"))
            {
                candidatePaths.Add(viewName);
            }
            else
            {
                // If contains a slash treat as relative to Views root once
                if (viewName.Contains('/'))
                {
                    candidatePaths.Add($"~/Views/{viewName}.cshtml");
                }
                else
                {
                    // Primary custom email folder
                    candidatePaths.Add($"~/Views/Invoices/EmailTemplates/{viewName}.cshtml");
                    // Shared email folder (optional future use)
                    candidatePaths.Add($"~/Views/Shared/Emails/{viewName}.cshtml");
                    // Direct under Views root fallback
                    candidatePaths.Add($"~/Views/{viewName}.cshtml");
                }
            }

            ViewEngineResult? viewResult = null;
            foreach (var path in candidatePaths)
            {
                viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: path, isMainPage: true);
                if (viewResult.Success) break;
            }

            if (viewResult == null || !viewResult.Success)
            {
                // Try FindView using the raw viewName (lets MVC search conventional locations)
                viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: true);
            }

            if (viewResult == null || !viewResult.Success || viewResult.View == null)
            {
                var searched = viewResult?.SearchedLocations ?? Array.Empty<string>();
                throw new InvalidOperationException($"View '{viewName}' not found. Tried: {string.Join(" | ", candidatePaths)}. Searched: {string.Join(" | ", searched)}");
            }

            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), controller?.ModelState ?? new ModelStateDictionary())
            {
                Model = model
            };

            var tempData = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);
            var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary, tempData, sw, new HtmlHelperOptions());
            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}
