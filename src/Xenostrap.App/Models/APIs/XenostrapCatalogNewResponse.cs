using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Xenostrap.Models.APIs
{
    public class XenostrapCatalogNewResponse
    {
        [JsonPropertyName("items")]
        public List<XenostrapCatalogItem> Items { get; set; } = new();
    }

    public class XenostrapCatalogItem
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public int? Price { get; set; }

        [JsonPropertyName("creator")]
        public string Creator { get; set; } = string.Empty;

        [JsonPropertyName("typeName")]
        public string TypeName { get; set; } = string.Empty;

        [JsonPropertyName("limited")]
        public bool Limited { get; set; }

        [JsonPropertyName("favorites")]
        public int Favorites { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; } = string.Empty;
    }
}
