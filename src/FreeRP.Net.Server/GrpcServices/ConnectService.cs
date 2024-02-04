using FreeRP.GrpcService.Connect;
using FreeRP.Net.Server.Data;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Server.GrpcServices
{
    public class ConnectService : GrpcService.Connect.ConnectService.ConnectServiceBase
    {
        private readonly FrpSettings _conf;
        private readonly ConnectResponse _res;

        public ConnectService(FrpSettings conf)
        {
            _conf = conf;

            _res = new ConnectResponse()
            {
                WithPassword = _conf.WithPassword,
                PasswordLength = _conf.PasswordLength,
                GrpcMessageSize = _conf.GrpcMessageSize
            };
        }

        public override Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
        {
            return Task.FromResult(_res);
        }

        public override Task<PingData> CheckConnection(PingData request, ServerCallContext context)
        {
            return Task.FromResult(request);
        }

        [Authorize]
        public override Task<PingData> CheckAuthorize(PingData request, ServerCallContext context)
        {
            return Task.FromResult(request);
        }
    }
}
