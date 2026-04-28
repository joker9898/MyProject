using Examine;
using Examine.Search;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;
using MyProject.Models;

namespace MyProject.Controllers
{
    [Route("umbraco/api/infocontent")]
    public class InfoContentController : UmbracoApiController
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

        // ============================================================
        //  GET /umbraco/api/infocontent/findnews
        //  Parameters: page, topic, country, from, to, lang
        // ============================================================
        [HttpGet("findnews")]
        public ActionResult<SearchViewModelData> FindNews(
            string? IndexPage = null,
            int page = 1,
            string? topic = null,
            string? country = null,
            string? from = null,
            string? to = null,
            string? lang = null)
        {
            // IndexPage (old style) ကိုလည်း ထောက်ပံ့သည်
            if (IndexPage != null && int.TryParse(IndexPage, out int pp))
                page = pp;

            if (!_examineManager.TryGetIndex(
                    Constants.UmbracoIndexes.ExternalIndexName, out IIndex index))
                throw new InvalidOperationException("Examine index not found.");

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

                // ── Filtering ──────────────────────────────────────
                var filtered = children.AsEnumerable();

                // Topic filter
                if (!string.IsNullOrEmpty(topic))
                    filtered = filtered.Where(x =>
                        (x.Value<string>("topic") ?? "")
                        .Contains(topic, StringComparison.OrdinalIgnoreCase));

                // Country filter
                if (!string.IsNullOrEmpty(country))
                    filtered = filtered.Where(x =>
                        (x.Value<string>("country") ?? "")
                        .Contains(country, StringComparison.OrdinalIgnoreCase));

                // From date filter
                if (!string.IsNullOrEmpty(from) && DateTime.TryParse(from, out DateTime fromDate))
                    filtered = filtered.Where(x =>
                        x.Value<DateTime?>("date") >= fromDate);

                // To date filter
                if (!string.IsNullOrEmpty(to) && DateTime.TryParse(to, out DateTime toDate))
                    filtered = filtered.Where(x =>
                        x.Value<DateTime?>("date") <= toDate.AddDays(1));

                // Language filter (culture)
                if (!string.IsNullOrEmpty(lang))
                    filtered = filtered.Where(x => x.IsVisible());

                // Culture filter — ထို culture မှာ published ဖြစ်တဲ့ items ပဲ ပြမည်
                if (!string.IsNullOrEmpty(lang))
                    filtered = filtered.Where(x =>
                    {
                        try { return x.IsPublished(lang); }
                        catch { return true; }
                    });

                // Sort by date descending
                var sorted = filtered
                    .OrderByDescending(x => x.Value<DateTime?>("date"))
                    .ToList();

                model.CurrentPage = page;
                model.TotalRecords = sorted.Count;
                model.TotalPages = Math.Ceiling((decimal)sorted.Count / pageSize);

                var paged = sorted
                    .Skip(pageSize * (page - 1))
                    .Take(pageSize);

                foreach (var child in paged)
                {
                    var img = child.Value<IPublishedContent>("bannerImage");

                    // ✅ Culture-specific name ယူသည်
                    // culture-specific name ကို အလွယ်ဆုံးနည်းဖြင့် ယူမည်
                    var cultureName = child.Name(lang) ?? child.Name;

                    model.NewsItemData.Add(new NewsItemData
                    {
                        titleNews = cultureName,   // ← ဒါပြောင်းပါ
                        description = child.Value<string>("description", culture: lang) ?? string.Empty,
                        url = !string.IsNullOrEmpty(lang)
                                        ? child.Url(culture: lang)
                                        : child.Url(),
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
