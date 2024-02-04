using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreeRP.Net.Server.Helpers
{
    public static class Json
    {
        private static JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string GetJson(object model) => JsonSerializer.Serialize(model, _options);
        public static T? GetModel<T>(string json) => JsonSerializer.Deserialize<T>(json, _options);
    }
}
