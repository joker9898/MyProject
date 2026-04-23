using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Models;

namespace MyProject.Controllers
{
    // ======================================================
    //  POST request အတွက် Request Body Model
    // ======================================================
    public class CreateServiceRequest
    {
        public string Name        { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // ======================================================
    //  ServicesApiController
    //  Routes:
    //    GET    /umbraco/api/services/GetServices
    //    POST   /umbraco/api/services/CreateService
    //    DELETE /umbraco/api/services/DeleteService/{id}
    // ======================================================
    [Route("umbraco/api/services/[action]")]
    public class ServicesApiController : UmbracoApiController
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IContentService         _contentService;

        // Parent page GUID — "News and Events" page
        private static readonly Guid ParentPageGuid =
            Guid.Parse("5de81975-a4ae-4273-9493-05f3a7cd4d63");

        // Document Type Alias — Umbraco Backoffice တွင် သတ်မှတ်ထားသော alias
        // ဥပမာ - "newsAndEventsItem" သို့မဟုတ် သင့် Document Type alias ကို ထည့်ပါ
        private const string DocumentTypeAlias = "newsAndEventsItem";

        public ServicesApiController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IContentService         contentService)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _contentService         = contentService;
        }

        // ============================================================
        //  GET /umbraco/api/services/GetServices
        //  ရည်ရွယ်ချက် - Parent page ရဲ့ Published child items အားလုံး ပြန်ပေးသည်
        // ============================================================
        [HttpGet]
        public IActionResult GetServices()
        {
            var umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();

            var servicesPage = umbracoContext.Content
                .GetById(ParentPageGuid);

            if (servicesPage == null)
                return NotFound(new { message = "Parent page not found." });

            var services = servicesPage.Children().Select(service => new
            {
                Id          = service.Id,
                Name        = service.Name,
                Description = service.Value<string>("description"),
                Url         = service.Url()
            });

            return Ok(services);
        }

        // ============================================================
        //  POST /umbraco/api/services/CreateService
        //  ရည်ရွယ်ချက် - Child item အသစ် တစ်ခု ဖန်တီးပြီး Publish လုပ်သည်
        //
        //  Request Body (JSON):
        //  {
        //    "name": "New Service Name",
        //    "description": "Service description here"
        //  }
        // ============================================================
        [HttpPost]
        public IActionResult CreateService([FromBody] CreateServiceRequest request)
        {
            // --- 1. Validation ---
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Name field is required." });

            // --- 2. Parent page ကို ID ဖြင့် ရှာသည် ---
            var umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var parentPage     = umbracoContext.Content.GetById(ParentPageGuid);

            if (parentPage == null)
                return NotFound(new { message = "Parent page not found." });

            // --- 3. Content အသစ် ဖန်တီးသည် ---
            var newContent = _contentService.Create(
                name:        request.Name,
                parentId:    parentPage.Id,
                contentTypeAlias: DocumentTypeAlias
            );

            // --- 4. Description field တန်ဖိုး သတ်မှတ်သည် ---
            newContent.SetValue("description", request.Description);

            // --- 5. Save and Publish လုပ်သည် ---
            var publishResult = _contentService.SaveAndPublish(newContent);

            if (!publishResult.Success)
                return StatusCode(500, new
                {
                    message = "Failed to create and publish content.",
                    reason  = publishResult.Result.ToString()
                });

            // --- 6. အောင်မြင်ပါက ဖန်တီးထားသော item ၏ အချက်အလက် ပြန်ပေးသည် ---
            return Ok(new
            {
                message     = "Service created successfully.",
                id          = newContent.Id,
                name        = newContent.Name,
                description = newContent.GetValue<string>("description")
            });
        }

        // ============================================================
        //  DELETE /umbraco/api/services/DeleteService/{id}
        //  ရည်ရွယ်ချက် - ID ဖြင့် child item တစ်ခု ဖျက်သည် (Recycle Bin သို့ ပို့သည်)
        //
        //  URL ဥပမာ: DELETE /umbraco/api/services/DeleteService/1234
        // ============================================================
        [HttpDelete("{id:int}")]
        public IActionResult DeleteService(int id)
        {
            // --- 1. Content item ကို ID ဖြင့် ရှာသည် ---
            var content = _contentService.GetById(id);

            if (content == null)
                return NotFound(new { message = $"Content with ID {id} not found." });

            // --- 2. Parent စစ်ဆေးသည် (မှားသော page မဖျက်မိအောင်) ---
            var umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var parentPage     = umbracoContext.Content.GetById(ParentPageGuid);

            if (parentPage == null || content.ParentId != parentPage.Id)
                return BadRequest(new
                {
                    message = "This item does not belong to the Services section. Delete cancelled."
                });

            // --- 3. Recycle Bin သို့ ရွှေ့သည် (MoveToRecycleBin = soft delete) ---
            var deleteResult = _contentService.MoveToRecycleBin(content);

            if (!deleteResult.Success)
                return StatusCode(500, new
                {
                    message = "Failed to delete content.",
                    reason  = deleteResult.Result.ToString()
                });

            return Ok(new
            {
                message = $"Service '{content.Name}' moved to Recycle Bin successfully.",
                id      = id
            });
        }
    }
}