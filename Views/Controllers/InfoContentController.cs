using Examine;
using Examine.Search;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Web.Common.Controllers;  // ← ဒါပြောင်းပါ
using Umbraco.Extensions;
using MyProject.Models;

namespace MyProject.Controllers
{
    [Route("umbraco/api/infocontent")]   // ← class level route
    public class InfoContentController : UmbracoApiController  // ← ControllerBase မဟုတ်တော့ဘူး
    {
        private readonly IPublishedContentQuery _publishedContent;
        private readonly IExamineManager _examineManager;

        public InfoContentController(
            IPublishedContentQuery publishedContent,
            IExamineManager examineManager)
        {
            _publishedContent = publishedContent;
            _examineManager = examineManager;
        }

        [HttpGet("findnews/{IndexPage?}")]
        public ActionResult<SearchViewModelData> FindNews(string? IndexPage)
        {
            if (!_examineManager.TryGetIndex(
                    Constants.UmbracoIndexes.ExternalIndexName, out IIndex index))
                throw new InvalidOperationException("Examine index not found.");

            int indexPage = 1;
            if (!string.IsNullOrEmpty(IndexPage) && int.TryParse(IndexPage, out int p))
                indexPage = p;

            int pageSize = 6;

            var query = index.Searcher.CreateQuery(IndexTypes.Content, BooleanOperation.And);
            var search = query.GroupedOr(
                new[] { "__NodeTypeAlias" },
                new[] { "newsAndEvents" }
            );

            var result = _publishedContent.Search(search);
            var model = new SearchViewModelData();

            foreach (var item in result)
            {
                var children = item.Content
                    .Children<IPublishedContent>()
                    ?.ToList();

                if (children == null || !children.Any()) continue;

                var sorted = children
                    .OrderByDescending(x => x.Value<DateTime?>("date"))
                    .ToList();

                model.CurrentPage = indexPage;
                model.TotalRecords = sorted.Count;
                model.TotalPages = Math.Ceiling((decimal)sorted.Count / pageSize);

                var paged = sorted
                    .Skip(pageSize * (indexPage - 1))
                    .Take(pageSize);

                foreach (var child in paged)
                {
                    var img = child.Value<IPublishedContent>("bannerImage");

                    model.NewsItemData.Add(new NewsItemData
                    {
                        titleNews = child.Name ?? string.Empty,
                        description = child.Value<string>("description") ?? string.Empty,
                        url = child.Url(),
                        imageResourceUrl = img?.Url() ?? string.Empty,
                        illustrationBackground = child.Value<string>("illustrationBackground") ?? "#f5f5f5",
                        lastUpdate = child.Value<DateTime?>("date")
                                                     ?.ToString("dd MMMM yyyy") ?? string.Empty,
                        published = child.Value<string>("published") ?? string.Empty,
                    });
                }
            }

            return Ok(model);
        }
    }
}