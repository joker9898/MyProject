using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Core.Models.PublishedContent; // ဒါအသစ်ထည့်ရပါမယ်

namespace MyProject.Controllers
{
    [Route("umbraco/api/services/[action]")]
    public class ServicesApiController : UmbracoApiController
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public ServicesApiController(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        [HttpGet]
        public IActionResult GetServices()
        {
            var umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();

            // 1234 အစား သင့် Page ရဲ့ ID အမှန်ကို ဒီမှာထည့်ပါ
            var servicesPage = umbracoContext.Content.GetById(Guid.Parse("5de81975-a4ae-4273-9493-05f3a7cd4d63"));

            if (servicesPage == null)
            {
                return NotFound("Page not found");
            }

            // .Children အစား .Children() ကို သုံးထားပါတယ်
            var services = servicesPage.Children().Select(service => new
            {
                Id = service.Id,
                Name = service.Name,
                // "description" ဆိုတာက Document Type ထဲက Alias name နဲ့ တူရပါမယ်
                Description = service.Value<string>("description")
            });

            return Ok(services);
        }       
    }   
}