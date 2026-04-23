namespace MyProject.Models
{
    // သတင်းတစ်ပုဒ်ချင်းစီအတွက် လိုအပ်တဲ့ အချက်အလက်များ
    public class NewsItemData
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public string Date { get; set; }
    }

    // API ကနေ အုပ်စုလိုက် ပြန်ပို့ပေးမယ့် Data ထုပ်ကြီး (Pagination အတွက်ပါ ပါတယ်)
    public class SearchViewModelData
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public List<NewsItemData> NewsItems { get; set; } = new List<NewsItemData>();
    }
}