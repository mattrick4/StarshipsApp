using System.Net.Http.Json;
using System.Text.Json.Serialization;
using StarshipsApp.Models;

namespace StarshipsApp.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(AppDbContext context, ILogger? logger = null, CancellationToken ct = default)
        {
            if (context.Starships.Any())
                return;

            var seeded = false;

            try
            {
                using var client = new HttpClient
                {
                    BaseAddress = new Uri("https://swapi.dev/api/"),
                    Timeout = TimeSpan.FromSeconds(10)
                };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("StarshipsApp/1.0");

                var all = new List<Starship>();
                string? url = "starships/"; // first page

                while (!ct.IsCancellationRequested && !string.IsNullOrWhiteSpace(url))
                {
                    var page = await client.GetFromJsonAsync<PagedResponse<Starship>>(url, ct);
                    if (page == null) break;

                    if (page.Results?.Count > 0)
                        all.AddRange(page.Results);

                    url = page.Next; // absolute URL or null
                }

                if (all.Count > 0)
                {
                    // Let EF assign IDs; ensure strings aren’t null
                    foreach (var s in all)
                    {
                        s.Name ??= "";
                        s.Model ??= "";
                        s.Manufacturer ??= "";
                        s.StarshipClass ??= "";
                        s.Crew ??= "";
                        s.Passengers ??= "";
                    }

                    context.Starships.AddRange(all);
                    await context.SaveChangesAsync(ct);
                    logger?.LogInformation("Seeded {Count} starships from SWAPI.", all.Count);
                    seeded = true;
                }
                else
                {
                    logger?.LogWarning("SWAPI returned no starships; falling back to embedded seed.");
                }
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is TaskCanceledException ||
                ex is NotSupportedException ||
                ex is System.Text.Json.JsonException)
            {
                logger?.LogWarning(ex, "SWAPI seed failed; falling back to embedded seed.");
            }

            if (!seeded)
            {
                var fallback = new List<Starship>
                {
                    new() { Name = "X-Wing", Model = "T-65B", Manufacturer = "Incom Corporation", StarshipClass = "Starfighter", Crew = "1", Passengers = "0" },
                    new() { Name = "Millennium Falcon", Model = "YT-1300", Manufacturer = "Corellian Engineering Corporation", StarshipClass = "Light Freighter", Crew = "2", Passengers = "6" },
                    new() { Name = "TIE Fighter", Model = "Twin Ion Engine/Ln", Manufacturer = "Sienar Fleet Systems", StarshipClass = "Starfighter", Crew = "1", Passengers = "0" }
                };

                context.Starships.AddRange(fallback);
                await context.SaveChangesAsync(ct);
                logger?.LogInformation("Seeded {Count} starships from embedded fallback.", fallback.Count);
            }
        }

        private sealed class PagedResponse<T>
        {
            [JsonPropertyName("next")]
            public string? Next { get; set; }
            [JsonPropertyName("results")]
            public List<T> Results { get; set; } = new();
        }
    }
}