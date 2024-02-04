extern alias fx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Test.Client.Api
{
    internal static class ApiClientTestSetting
    {
        public static readonly fx::FreeRP.Net.Client.Translation.I18nService I18n = new();
        public static readonly fx::FreeRP.Net.Client.Services.ConnectService ConnectService = new();

        public static async Task<bool> ConnectToServerAsync()
        {
            return await ConnectService.TryConnectAsync(ClientTestSettings.ServerUrl);
        }

        public static async Task CleanUpAsync()
        {
            await ConnectService.TryDisconnectAsync();
        }
    }
}
