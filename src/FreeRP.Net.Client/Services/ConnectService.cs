using Grpc.Net.Client.Web;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FreeRP.Net.Client.Services
{
    public class ConnectService
    {
        public bool IsConnected { get; set; }
        public GrpcService.Core.LoginResponse LoginResponse { get; set; } = new();
        public GrpcService.Connect.ConnectResponse ConnectResponse { get; set; } = new();

        public GrpcService.Content.ContentService.ContentServiceClient ContentServiceClient { get; set; } = default!;
        public GrpcService.Admin.AdminService.AdminServiceClient AdminServiceClient { get; set; } = default!;
        public GrpcService.User.UserService.UserServiceClient UserServiceClient { get; set; } = default!;
        public GrpcService.Database.DatabaseService.DatabaseServiceClient DatabaseServiceClient { get; set; } = default!;

        public string ServerUrl { get; set; } = string.Empty;
        public GrpcChannel? GrpcChannel { get; set; }
        
        public Grpc.Core.Metadata AuthHeader { get; set; } = [];

        private GrpcService.Connect.ConnectService.ConnectServiceClient? _client;
        

        public async Task<bool> TryConnectAsync(string url)
        {
            if (url.StartsWith("http", StringComparison.CurrentCultureIgnoreCase) == false)
            {
                url = $"https://{url}";
            }

            if (url.EndsWith('/') == false)
                url += "/" ;

            if (IsConnected && url == ServerUrl)
                return true;

            try
            {
                if (GrpcChannel != null)
                {
                    await TryDisconnectAsync();
                }

                GrpcChannel = GrpcChannel.ForAddress(url, new GrpcChannelOptions
                {
                    HttpHandler = new GrpcWebHandler(new HttpClientHandler())
                });
                
                _client = new GrpcService.Connect.ConnectService.ConnectServiceClient(GrpcChannel);
                ConnectResponse = await _client.ConnectAsync(new GrpcService.Connect.ConnectRequest());

                AdminServiceClient = new(GrpcChannel);
                UserServiceClient = new(GrpcChannel);
                DatabaseServiceClient = new(GrpcChannel);
                ContentServiceClient = new(GrpcChannel);

                ServerUrl = url;
                
                IsConnected = true;
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        public async Task TryDisconnectAsync()
        {
            if (GrpcChannel != null)
            {
                try
                {
                    await GrpcChannel.ShutdownAsync();
                    GrpcChannel.Dispose();
                }
                catch (Exception)
                {
                }
            }

            GrpcChannel = null;
        }
    }
}
