using System.Text.Json.Serialization;

namespace StarshipsApp.Models
{
    // Starship model representing a starship object
    public class Starship
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Model { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        [JsonPropertyName("starship_class")]
        public string StarshipClass { get; set; } = "";
        public string Crew { get; set; } = "";
        public string Passengers { get; set; } = "";
        public string? Url { get; set; }
    }
}