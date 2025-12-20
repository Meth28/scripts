using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CanaryLauncherUpdate
{
    public class CountdownEvent
    {
        public string Name { get; set; } = "";
        public DateTime EndTime { get; set; }
        public long TimestampMs { get; set; }

        public TimeSpan GetRemainingTime()
        {
            var now = DateTime.Now;
            return EndTime > now ? EndTime - now : TimeSpan.Zero;
        }

        public string GetFormattedRemainingTime()
        {
            var remaining = GetRemainingTime();

            if (remaining == TimeSpan.Zero)
                return "Event started!";

            if (remaining.Days > 0)
                return $"{remaining.Days}d {remaining.Hours}h {remaining.Minutes}m";
            else
                return $"{remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s";
        }
    }

    public class CountdownService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static DateTime lastFetchTime = DateTime.MinValue;
        private static List<CountdownEvent> cachedCountdowns = new List<CountdownEvent>();
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

        // Cambia esta URL por la ruta correcta de tu archivo events.txt
        private const string EVENTS_TXT_URL = "http://luneraot.com/events.txt";

        static CountdownService()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public static async Task<List<CountdownEvent>> FetchCountdownsAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && DateTime.Now - lastFetchTime < CACHE_DURATION && cachedCountdowns.Count > 0)
                return cachedCountdowns;

            try
            {
                var countdowns = await LoadEventsFromTxtAsync();
                cachedCountdowns = countdowns;
                lastFetchTime = DateTime.Now;
                return countdowns;
            }
            catch
            {
                return cachedCountdowns.Count > 0 ? cachedCountdowns : new List<CountdownEvent>();
            }
        }

        private static async Task<List<CountdownEvent>> LoadEventsFromTxtAsync()
        {
            var list = new List<CountdownEvent>();

            var response = await httpClient.GetAsync(EVENTS_TXT_URL);
            if (!response.IsSuccessStatusCode)
                return list; // vacío si no pudo cargar

            string content = await response.Content.ReadAsStringAsync();

            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // El archivo debe tener pares de líneas: nombre y fecha
            for (int i = 0; i < lines.Length - 1; i += 2)
            {
                string name = lines[i].Trim();
                string dateStr = lines[i + 1].Trim();

                // Parseamos la fecha en formato dd/MM/yyyy (ejemplo: 25/07/2025)
                if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                {
                    DateTime endTime = parsedDate.Date;

                    list.Add(new CountdownEvent
                    {
                        Name = name,
                        EndTime = endTime,
                        TimestampMs = new DateTimeOffset(endTime).ToUnixTimeMilliseconds()
                    });
                }
            }

            return list;
        }
    }
}
