﻿using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Helpers
{
    public class GrpcJson
    {
        private static JsonFormatter.Settings _sfs = JsonFormatter.Settings.Default;
        private static JsonParser.Settings _sps = JsonParser.Settings.Default
            .WithIgnoreUnknownFields(true);

        private static JsonFormatter _jf = new(_sfs);
        private static JsonParser _jp = new(_sps);
        public static T? GetModel<T>(string? json) where T : IMessage, new()
        {
            if (json is null)
            {
                return default;
            }

            return _jp.Parse<T>(json);
        }
        public static string GetJson(IMessage model) => _jf.Format(model);
    }
}