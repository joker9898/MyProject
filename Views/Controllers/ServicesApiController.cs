using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;
using MyProject.Models;

namespace MyProject.Controllers
{
    public class CreateServiceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ServicesApiController : UmbracoApiController
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IContentService _contentService;

        private static readonly Guid ServicesPageGuid = Guid.Parse("513b1e17-78a1-4431-9f93-409c35af373a");
        private static readonly Guid NewsPageGuid = Guid.Parse("5de81975-a4ae-4273-9493-05f3a7cd4d63");

        public ServicesApiController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IContentService contentService)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _contentService = contentService;
        }

        // GET /umbraco/api/servicesapi/getservices
        [HttpGet]
        public IActionResult GetServices()
        {
            var umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var servicesPage = umbracoContext.Content.GetById(ServicesPageGuid);

            if (servicesPage == null)
                return NotFound(new { message = "Services page not found." });

            var services = servicesPage.Children().Select(service => new
            {
                id = service.Id,          // ✅ lowercase — JS နဲ့ ကိုက်ညီ
                name = service.Name,
                description = service.Value<string>("description"),
                url = service.Url()
            });

            return Ok(services);
        }

        // GET /umbraco/api/servicesapi/getnews?page=1&lang=en-US
        [HttpGet]
        public IActionResult GetNews(int page = 1, string lang = "en-US")
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
                return BadRequest();

            var newsLandingPage = context.Content.GetById(NewsPageGuid);
            if (newsLandingPage == null)
                return NotFound(new { message = "News and Events page not found." });

            // ✅ Children() — ? ဖြုတ်ထားသည်
            var allNews = newsLandingPage.Children()
                .Where(x => x.IsVisible())
                .OrderByDescending(x => x.CreateDate)
                .ToList();

            int pageSize = 3;
            int totalItems = allNews.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var pagedNews = allNews.Skip((page - 1) * pageSize).Take(pageSize);

            var newsItems = pagedNews.Select(item =>
            {
                var img = item.Value<IPublishedContent>("bannerImage");
                return new
                {
                    title = item.Name,
                    description = item.Value<string>("description", culture: lang) ?? "",
                    url = item.Url(culture: lang),
                    imageUrl = img?.Url() ?? ""
                };
            });

            return Ok(new { newsItems, totalPages });
        }

        // POST /umbraco/api/servicesapi/createservice
        [HttpPost]
        public IActionResult CreateService([FromBody] CreateServiceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Name field is required." });

            var parentContent = _contentService.GetById(ServicesPageGuid);
            if (parentContent == null)
                return NotFound(new { message = "Parent page not found." });

            var newContent = _contentService.Create(request.Name, parentContent.Id, "itemProduct");
            newContent.SetValue("description", request.Description);
            _contentService.Save(newContent);

            var publishResult = _contentService.Publish(newContent, new[] { "*" });
            if (!publishResult.Success)
                return StatusCode(500, new { message = "Publish failed." });

            return Ok(new { message = "Service created successfully.", id = newContent.Id });
        }

        // DELETE /umbraco/api/servicesapi/deleteservice/{id}
        [HttpDelete]
        public IActionResult DeleteService(int id)
        {
            var content = _contentService.GetById(id);
            if (content == null)
                return NotFound(new { message = $"ID {id} not found." });

            _contentService.MoveToRecycleBin(content);
            return Ok(new { message = $"ID {id} moved to Recycle Bin." });
        }
    }
}