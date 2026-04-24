namespace MyProject.Models
{
    public class NewsItemData
    {
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string imageUrl { get; set; } = string.Empty;
        public string date { get; set; } = string.Empty;
        public string published { get; set; } = string.Empty;
        public string lastUpdate { get; set; } = string.Empty;
        public string imageResourceUrl { get; set; } = string.Empty;
        public string illustrationBackground { get; set; } = string.Empty;
        public string titleNews { get; set; } = string.Empty;
    }

    public class SearchViewModelData
    {
        public SearchViewModelData()
        {
            this.NewsItemData = new List<NewsItemData>();
        }
        public long TotalRecords { get; set; }
        public decimal TotalPages { get; set; }
        public long CurrentPage { get; set; }
        public List<NewsItemData> NewsItemData { get; set; }
    }
}