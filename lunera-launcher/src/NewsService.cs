using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;

namespace CanaryLauncherUpdate
{
    public class NewsItem
    {
        public string Title { get; set; }
        public string Date { get; set; }
        public string Content { get; set; }
        public string Url { get; set; }
        public string IconType { get; set; }
    }

    public class NewsService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string NEWS_ARCHIVE_URL = "https://luneraot.com/?news/archive";
        private const string BASE_URL = "https://luneraot.com";

        static NewsService()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public static async Task<List<NewsItem>> FetchNewsAsync()
        {
            try
            {
                var newsItems = new List<NewsItem>();
                
                // Fetch the news archive page
                string archiveHtml = await httpClient.GetStringAsync(NEWS_ARCHIVE_URL);
                
                // Use a simpler, more reliable pattern to find news items
                var newsMatches = Regex.Matches(archiveHtml,
                    @"<tr[^>]*>.*?icon_(\d+)_small\.gif.*?(\d+\.\d+\.\d+).*?href=""([^""]*)"">([^<]+)</a>.*?</tr>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                foreach (Match match in newsMatches)
                {
                    if (match.Groups.Count >= 5)
                    {
                        var newsItem = new NewsItem
                        {
                            IconType = match.Groups[1].Value,
                            Date = match.Groups[2].Value.Trim(),
                            Url = match.Groups[3].Value,
                            Title = match.Groups[4].Value.Trim()
                        };

                        // Fetch the full content for this news item
                        try
                        {
                            string fullUrl = newsItem.Url.StartsWith("http") ? newsItem.Url : BASE_URL + "/" + newsItem.Url.TrimStart('?');
                            newsItem.Content = await FetchNewsContentAsync(fullUrl);
                        }
                        catch
                        {
                            newsItem.Content = $"📰 {newsItem.Title}\n📅 {newsItem.Date}\n\nClick to read the full article...";
                        }

                        newsItems.Add(newsItem);
                    }
                }

                return newsItems.Take(3).ToList(); // Return only the latest 3 news items
            }
            catch (Exception)
            {
                // Return fallback news if fetching fails
                return GetFallbackNews();
            }
        }

        private static async Task<string> FetchNewsContentAsync(string url)
        {
            try
            {
                string html = await httpClient.GetStringAsync(url);
                
                // Look for the news content in the table with padding
                var contentMatch = Regex.Match(html, 
                    @"<td[^>]*style=""padding-left:10px;padding-right:10px;""[^>]*><p>(.*?)</p></td>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (contentMatch.Success)
                {
                    string content = contentMatch.Groups[1].Value;
                    
                    // Clean up HTML tags and format for display
                    content = Regex.Replace(content, @"<br\s*/?>", " ", RegexOptions.IgnoreCase);
                    content = Regex.Replace(content, @"<img[^>]*>", "", RegexOptions.IgnoreCase);
                    content = Regex.Replace(content, @"<[^>]+>", "", RegexOptions.IgnoreCase);
                    content = System.Net.WebUtility.HtmlDecode(content);
                    content = Regex.Replace(content, @"\s+", " ", RegexOptions.Multiline);
                    content = content.Trim();
                    
                    // Limit content length for display
                    if (content.Length > 150)
                    {
                        content = content.Substring(0, 150) + "...";
                    }
                    
                    return content;
                }
                
                return "Click to read the full article...";
            }
            catch
            {
                return "Click to read the full article...";
            }
        }

        private static List<NewsItem> GetFallbackNews()
        {
            return new List<NewsItem>
            {
                new NewsItem
                {
                    Title = "Welcome to LuneraOT!",
                    Date = DateTime.Now.ToString("dd.MM.yyyy"),
                    Content = "🎮 New Features:\n• Enhanced Battle Royale system\n• 1 vs 1 duels with ranking\n• New PvP zones and events\n• Renovated guild system\n\n⚡ Recent Updates:\n• Improved class balance\n• New epic items and equipment\n• Performance optimization\n• Critical bug fixes",
                    IconType = "0"
                },
                new NewsItem
                {
                    Title = "Server Updates",
                    Date = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"),
                    Content = "📅 Upcoming Events:\n• Guild tournament this weekend\n• Double experience event\n• New epic quest available\n\n⚠️ Important:\nLuneraOT can be dangerous. Stay alert!",
                    IconType = "3"
                }
            };
        }

        public static string FormatNewsForDisplay(List<NewsItem> newsItems)
        {
            if (newsItems == null || !newsItems.Any())
            {
                return "No news available at the moment.";
            }

            var formattedNews = new List<string>();

            for (int i = 0; i < newsItems.Count; i++)
            {
                var item = newsItems[i];
                string emoji = GetEmojiForIconType(item.IconType);
                formattedNews.Add($"[{i + 1}] {emoji} {item.Title}\n{item.Date}\n\n{item.Content}\n\n🔗 Click to read full article");
            }

            return string.Join("\n\n" + new string('═', 35) + "\n\n", formattedNews);
        }

        public static string FormatNewsForDisplayWithHighlight(List<NewsItem> newsItems, int highlightIndex)
        {
            if (newsItems == null || !newsItems.Any())
            {
                return "No news available at the moment.";
            }

            var formattedNews = new List<string>();

            for (int i = 0; i < newsItems.Count; i++)
            {
                var item = newsItems[i];
                string emoji = GetEmojiForIconType(item.IconType);
                string prefix = i == highlightIndex ? "► " : "  ";
                string clickText = i == highlightIndex ? "🔗 NEXT: Click to open this article" : "🔗 Click to read full article";
                formattedNews.Add($"{prefix}[{i + 1}] {emoji} {item.Title}\n{item.Date}\n\n{item.Content}\n\n{clickText}");
            }

            return string.Join("\n\n" + new string('═', 35) + "\n\n", formattedNews);
        }

        private static string GetEmojiForIconType(string iconType)
        {
            switch (iconType)
            {
                case "0":
                    return "🏆"; // General news
                case "1":
                    return "📢"; // Announcements
                case "2":
                    return "⚔️"; // PvP/Combat
                case "3":
                    return "🎉"; // Events
                case "4":
                    return "🔧"; // Technical updates
                default:
                    return "📰"; // Default
            }
        }
    }
}