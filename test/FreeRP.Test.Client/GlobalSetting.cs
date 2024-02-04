using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Test.Client
{
    internal static class GlobalSetting
    {
        public const string ServerUrl = "https://localhost:7127";
        public static readonly Net.Client.Translation.I18nService I18n = new();
        public static readonly Net.Client.Services.ConnectService ConnectService = new();

        public static async Task<bool> ConnectToServerAsync()
        {
            return await ConnectService.TryConnectAsync(ServerUrl);
        }

        public static async Task CleanUpAsync()
        {
            await ConnectService.TryDisconnectAsync();
        }
    }
}
