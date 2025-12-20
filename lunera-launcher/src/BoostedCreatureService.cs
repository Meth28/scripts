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
        public int CreatureId { get; set; }
        public int Addons { get; set; }
        public int Head { get; set; }
        public int Body { get; set; }
        public int Legs { get; set; }
        public int Feet { get; set; }
        public int Mount { get; set; }
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
                // Try more flexible regex patterns
                var creaturePattern = @"id=""Creature""[^>]*src=""[^""]*animoutfit\.php\?id=(\d+)&addons=(\d+)&head=(\d+)&body=(\d+)&legs=(\d+)&feet=(\d+)&mount=(\d+)""[^>]*title=""[^:]*:\s*([^""]+)""";
                var creatureMatch = Regex.Match(html, creaturePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                
                if (creatureMatch.Success)
                {
                    creature = new BoostedCreature
                    {
                        CreatureId = int.Parse(creatureMatch.Groups[1].Value),
                        Addons = int.Parse(creatureMatch.Groups[2].Value),
                        Head = int.Parse(creatureMatch.Groups[3].Value),
                        Body = int.Parse(creatureMatch.Groups[4].Value),
                        Legs = int.Parse(creatureMatch.Groups[5].Value),
                        Feet = int.Parse(creatureMatch.Groups[6].Value),
                        Mount = int.Parse(creatureMatch.Groups[7].Value),
                        Name = creatureMatch.Groups[8].Value.Trim(),
                        Type = "Creature",
                        ImageUrl = $"{BASE_URL}/images/animated-outfits/animoutfit.php?id={creatureMatch.Groups[1].Value}&addons={creatureMatch.Groups[2].Value}&head={creatureMatch.Groups[3].Value}&body={creatureMatch.Groups[4].Value}&legs={creatureMatch.Groups[5].Value}&feet={creatureMatch.Groups[6].Value}&mount={creatureMatch.Groups[7].Value}"
                    };
                }

                var bossPattern = @"id=""Boss""[^>]*src=""[^""]*animoutfit\.php\?id=(\d+)&addons=(\d+)&head=(\d+)&body=(\d+)&legs=(\d+)&feet=(\d+)&mount=(\d+)""[^>]*title=""[^:]*:\s*([^""]+)""";
                var bossMatch = Regex.Match(html, bossPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                
                if (bossMatch.Success)
                {
                    boss = new BoostedCreature
                    {
                        CreatureId = int.Parse(bossMatch.Groups[1].Value),
                        Addons = int.Parse(bossMatch.Groups[2].Value),
                        Head = int.Parse(bossMatch.Groups[3].Value),
                        Body = int.Parse(bossMatch.Groups[4].Value),
                        Legs = int.Parse(bossMatch.Groups[5].Value),
                        Feet = int.Parse(bossMatch.Groups[6].Value),
                        Mount = int.Parse(bossMatch.Groups[7].Value),
                        Name = bossMatch.Groups[8].Value.Trim(),
                        Type = "Boss",
                        ImageUrl = $"{BASE_URL}/images/animated-outfits/animoutfit.php?id={bossMatch.Groups[1].Value}&addons={bossMatch.Groups[2].Value}&head={bossMatch.Groups[3].Value}&body={bossMatch.Groups[4].Value}&legs={bossMatch.Groups[5].Value}&feet={bossMatch.Groups[6].Value}&mount={bossMatch.Groups[7].Value}"
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
                // Extract boosted creature information - updated regex to match actual HTML structure
                var creatureMatch = Regex.Match(html, 
                    @"<img\s+id=""Creature""\s+src=""images/animated-outfits/animoutfit\.php\?id=(\d+)&addons=(\d+)&head=(\d+)&body=(\d+)&legs=(\d+)&feet=(\d+)&mount=(\d+)""\s+alt=""[^""]*""\s+title=""Today's boosted creature:\s*([^""]+)""",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                if (creatureMatch.Success)
                {
                    return new BoostedCreature
                    {
                        CreatureId = int.Parse(creatureMatch.Groups[1].Value),
                        Addons = int.Parse(creatureMatch.Groups[2].Value),
                        Head = int.Parse(creatureMatch.Groups[3].Value),
                        Body = int.Parse(creatureMatch.Groups[4].Value),
                        Legs = int.Parse(creatureMatch.Groups[5].Value),
                        Feet = int.Parse(creatureMatch.Groups[6].Value),
                        Mount = int.Parse(creatureMatch.Groups[7].Value),
                        Name = creatureMatch.Groups[8].Value.Trim(),
                        Type = "Creature",
                        ImageUrl = $"{BASE_URL}/images/animated-outfits/animoutfit.php?id={creatureMatch.Groups[1].Value}&addons={creatureMatch.Groups[2].Value}&head={creatureMatch.Groups[3].Value}&body={creatureMatch.Groups[4].Value}&legs={creatureMatch.Groups[5].Value}&feet={creatureMatch.Groups[6].Value}&mount={creatureMatch.Groups[7].Value}"
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
                // Extract boosted boss information - updated regex to match actual HTML structure
                var bossMatch = Regex.Match(html, 
                    @"<img\s+id=""Boss""\s+src=""images/animated-outfits/animoutfit\.php\?id=(\d+)&addons=(\d+)&head=(\d+)&body=(\d+)&legs=(\d+)&feet=(\d+)&mount=(\d+)""\s+alt=""[^""]*""\s+title=""Today's boosted boss:\s*([^""]+)""",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                if (bossMatch.Success)
                {
                    return new BoostedCreature
                    {
                        CreatureId = int.Parse(bossMatch.Groups[1].Value),
                        Addons = int.Parse(bossMatch.Groups[2].Value),
                        Head = int.Parse(bossMatch.Groups[3].Value),
                        Body = int.Parse(bossMatch.Groups[4].Value),
                        Legs = int.Parse(bossMatch.Groups[5].Value),
                        Feet = int.Parse(bossMatch.Groups[6].Value),
                        Mount = int.Parse(bossMatch.Groups[7].Value),
                        Name = bossMatch.Groups[8].Value.Trim(),
                        Type = "Boss",
                        ImageUrl = $"{BASE_URL}/images/animated-outfits/animoutfit.php?id={bossMatch.Groups[1].Value}&addons={bossMatch.Groups[2].Value}&head={bossMatch.Groups[3].Value}&body={bossMatch.Groups[4].Value}&legs={bossMatch.Groups[5].Value}&feet={bossMatch.Groups[6].Value}&mount={bossMatch.Groups[7].Value}"
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
                CreatureId = 0,
                Addons = 0,
                Head = 0,
                Body = 0,
                Legs = 0,
                Feet = 0,
                Mount = 0,
                ImageUrl = null
            };
        }

        private static BoostedCreature GetFallbackBoss()
        {
            return new BoostedCreature
            {
                Name = "Loading...",
                Type = "Boss",
                CreatureId = 0,
                Addons = 0,
                Head = 0,
                Body = 0,
                Legs = 0,
                Feet = 0,
                Mount = 0,
                ImageUrl = null
            };
        }
    }
}