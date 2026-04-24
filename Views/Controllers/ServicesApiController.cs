using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using Umbraco.Extensions;
using MyProject.Models;

namespace MyProject.Controllers
{
    public class CreateServiceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // ======================================================
    //  ServicesApiController

    public class ServicesApiController : UmbracoApiController
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IContentService _contentService;

        // Services (What We Do) Page ၏ GUID
        private static readonly Guid ServicesPageGuid = Guid.Parse("513b1e17-78a1-4431-9f93-409c35af373a");

        // News and Events Page ၏ GUID
        private static readonly Guid NewsPageGuid = Guid.Parse("5de81975-a4ae-4273-9493-05f3a7cd4d63");

        // News & Events Document Type Alias
        private const string NewsItemAlias = "newsAndEventsItem";

        public ServicesApiController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IContentService contentService)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _contentService = contentService;
        }

        // ============================================================
        //  GET /umbraco/api/services/getservices
        // ============================================================
        [HttpGet]
        public IActionResult GetServices()
        {
            var umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var servicesPage = umbracoContext.Content.GetById(ServicesPageGuid);

            if (servicesPage == null)
                return NotFound(new { message = "Services page not found." });

            var services = servicesPage.Children().Select(service => new
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Value<string>("description"),
                Url = service.Url()
            });

            return Ok(services);
        }

        // ============================================================
        //  GET /umbraco/api/servicesapi/getnews?page=1&lang=en-US
        // ============================================================
        [HttpGet]
        public IActionResult GetNews(int page = 1, string lang = "en-US")
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
                return BadRequest();

            // ✅ GUID သုံး — hardcoded ID မသုံးပါ
            var newsLandingPage = context.Content.GetById(NewsPageGuid);
            if (newsLandingPage == null)
                return NotFound(new { message = "News and Events page not found." });

            var allNews = newsLandingPage.Children?
                .Where(x => x.IsVisible())
                .OrderByDescending(x => x.CreateDate)
                .ToList() ?? new List<IPublishedContent>();

            int pageSize = 3;
            var totalItems = allNews.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var pagedNews = allNews.Skip((page - 1) * pageSize).Take(pageSize);

            var newsItems = pagedNews.Select(item =>
            {
                var img = item.Value<IPublishedContent>("bannerImage");

                return new
                {
                    title = item.Name,                                          // ✅ Name က property သာဖြစ်သည်
                    description = item.Value<string>("description", culture: lang) ?? "",
                    url = item.Url(culture: lang),
                    imageUrl = img?.Url() ?? ""
                };
            });

            return Ok(new { newsItems, totalPages });
        }

        // ============================================================
        //  POST /umbraco/api/services/createservice
        // ============================================================
        [HttpPost("/umbraco/api/services/createservice")]
        public IActionResult CreateService([FromBody] CreateServiceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Name field is required." });

            var parentContent = _contentService.GetById(ServicesPageGuid);
            if (parentContent == null)
                return NotFound(new { message = "Parent page not found." });

            // မှတ်ချက် - DocumentTypeAlias ကို အခြေအနေအရ စစ်ဆေးရန်
            var newContent = _contentService.Create(request.Name, parentContent.Id, "itemProduct");

            newContent.SetValue("description", request.Description);
            _contentService.Save(newContent);
            var publishResult = _contentService.Publish(newContent, new[] { "*" });


            if (!publishResult.Success)
                return StatusCode(500, new { message = "Publish failed." });

            return Ok(new { message = "Success", id = newContent.Id });
        }

        // ============================================================
        //  DELETE /umbraco/api/services/deleteservice/{id}
        // ============================================================
        [HttpDelete("/umbraco/api/services/deleteservice/{id:int}")]
        public IActionResult DeleteService(int id)
        {
            var content = _contentService.GetById(id);
            if (content == null) return NotFound();

            _contentService.MoveToRecycleBin(content);
            return Ok(new { message = "Deleted successfully" });
        }
    }
}