using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Helpers
{
    public static class Json
    {
        private static System.Text.Json.JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public static string GetJson(object model) => System.Text.Json.JsonSerializer.Serialize(model);
        public static T? GetModel<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json, options);
    }
}
