using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CanaryLauncherUpdate
{
    public class BoostedCreature
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Type { get; set; } // "Creature" or "Boss"
    }

    public class BoostedCreatureService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string BASE_URL = "http://luneraot.com";
        private static DateTime lastFetchTime = DateTime.MinValue;
        private static (BoostedCreature creature, BoostedCreature boss) cachedResults;
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5); // Cache for 5 minutes

        static BoostedCreatureService()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(10); // 10 second timeout
        }

        public static async Task<(BoostedCreature creature, BoostedCreature boss)> FetchBoostedCreaturesAsync(bool forceRefresh = false)
        {
            // Check cache first (unless force refresh is requested)
            if (!forceRefresh && DateTime.Now - lastFetchTime < CACHE_DURATION && cachedResults.creature != null)
            {
                return cachedResults;
            }

            try
            {
                string html = await httpClient.GetStringAsync(BASE_URL);
                
                var creature = ExtractBoostedCreature(html);
                var boss = ExtractBoostedBoss(html);
                
                // If both extractions failed (returned fallback values), try alternative parsing
                if (creature.Name == "Loading..." && boss.Name == "Loading...")
                {
                    // Try alternative extraction methods
                    var alternativeResults = TryAlternativeExtraction(html);
                    if (alternativeResults.creature != null || alternativeResults.boss != null)
                    {
                        creature = alternativeResults.creature ?? creature;
                        boss = alternativeResults.boss ?? boss;
                    }
                }
                
                // Cache the results
                cachedResults = (creature, boss);
                lastFetchTime = DateTime.Now;
                
                return (creature, boss);
            }
            catch (Exception)
            {
                // If we have cached results, return them even if they're old
                if (cachedResults.creature != null)
                {
                    return cachedResults;
                }
                
                // Return fallback data if fetching fails and no cache
                return GetFallbackBoostedCreatures();
            }
        }

        public static async Task<(BoostedCreature creature, BoostedCreature boss)> ForceRefreshBoostedCreaturesAsync()
        {
            return await FetchBoostedCreaturesAsync(forceRefresh: true);
        }

        private static (BoostedCreature creature, BoostedCreature boss) TryAlternativeExtraction(string html)
        {
            BoostedCreature creature = null;
            BoostedCreature boss = null;

            try
            {
                // Try more flexible regex patterns for creature
                var creaturePattern = @"id=""Creature""[^>]*src=""([^""]*animoutfit\.php[^""]*)""[^>]*title=""[^:]*:\s*([^""]+)""";
                var creatureMatch = Regex.Match(html, creaturePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                
                if (creatureMatch.Success)
                {
                    creature = new BoostedCreature
                    {
                        Name = creatureMatch.Groups[2].Value.Trim(),
                        Type = "Creature",
                        ImageUrl = creatureMatch.Groups[1].Value.StartsWith("http") ? creatureMatch.Groups[1].Value : BASE_URL + "/" + creatureMatch.Groups[1].Value.TrimStart('/')
                    };
                }

                // Try more flexible regex patterns for boss
                var bossPattern = @"id=""Boss""[^>]*src=""([^""]*animoutfit\.php[^""]*)""[^>]*title=""[^:]*:\s*([^""]+)""";
                var bossMatch = Regex.Match(html, bossPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                
                if (bossMatch.Success)
                {
                    boss = new BoostedCreature
                    {
                        Name = bossMatch.Groups[2].Value.Trim(),
                        Type = "Boss",
                        ImageUrl = bossMatch.Groups[1].Value.StartsWith("http") ? bossMatch.Groups[1].Value : BASE_URL + "/" + bossMatch.Groups[1].Value.TrimStart('/')
                    };
                }
            }
            catch (Exception)
            {
                // Ignore alternative extraction errors
            }

            return (creature, boss);
        }

        private static BoostedCreature ExtractBoostedCreature(string html)
        {
            try
            {
                // Extract boosted creature information - simplified to just get the image URL and name
                var creatureMatch = Regex.Match(html, 
                    @"<img\s+id=""Creature""\s+src=""(images/animated-outfits/animoutfit\.php[^""]*)""\s+alt=""[^""]*""\s+title=""Today's boosted creature:\s*([^""]+)""",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                if (creatureMatch.Success)
                {
                    return new BoostedCreature
                    {
                        Name = creatureMatch.Groups[2].Value.Trim(),
                        Type = "Creature",
                        ImageUrl = $"{BASE_URL}/{creatureMatch.Groups[1].Value}"
                    };
                }
            }
            catch (Exception)
            {
                // Ignore parsing errors
            }

            return GetFallbackCreature();
        }

        private static BoostedCreature ExtractBoostedBoss(string html)
        {
            try
            {
                // Extract boosted boss information - simplified to just get the image URL and name
                var bossMatch = Regex.Match(html, 
                    @"<img\s+id=""Boss""\s+src=""(images/animated-outfits/animoutfit\.php[^""]*)""\s+alt=""[^""]*""\s+title=""Today's boosted boss:\s*([^""]+)""",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                if (bossMatch.Success)
                {
                    return new BoostedCreature
                    {
                        Name = bossMatch.Groups[2].Value.Trim(),
                        Type = "Boss",
                        ImageUrl = $"{BASE_URL}/{bossMatch.Groups[1].Value}"
                    };
                }
            }
            catch (Exception)
            {
                // Ignore parsing errors
            }

            return GetFallbackBoss();
        }

        private static (BoostedCreature creature, BoostedCreature boss) GetFallbackBoostedCreatures()
        {
            return (GetFallbackCreature(), GetFallbackBoss());
        }

        private static BoostedCreature GetFallbackCreature()
        {
            return new BoostedCreature
            {
                Name = "Loading...",
                Type = "Creature",
                ImageUrl = null
            };
        }

        private static BoostedCreature GetFallbackBoss()
        {
            return new BoostedCreature
            {
                Name = "Loading...",
                Type = "Boss",
                ImageUrl = null
            };
        }
    }
}