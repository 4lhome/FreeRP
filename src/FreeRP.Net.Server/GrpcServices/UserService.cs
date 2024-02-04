using FreeRP.GrpcService.Core;
using FreeRP.GrpcService.User;
using FreeRP.Net.Server.Data;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Server.GrpcServices
{
    public class UserService(IFrpDataService appData, FrpSettings appConfigService, AuthService authService) : GrpcService.User.UserService.UserServiceBase
    {
        private readonly IFrpDataService _appData = appData;
        private readonly FrpSettings _appConfig = appConfigService;
        private readonly AuthService _authService = authService;

        public override Task<LoginResponse> Login(User request, ServerCallContext context)
        {
            if (_appData.UserGetByEmail(request.Email) is User u)
            {
                request.UserId = u.UserId;
                if (_appData.UserCheckPassword(request))
                {
                    return Task.FromResult(new LoginResponse()
                    {
                        ExpirationDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow.AddHours(_appConfig.JwtExpireHours)),
                        Token = _authService.GenerateJwtToken(request, false),
                        User = u
                    });
                }
            }

            return Task.FromResult(new LoginResponse());
        }

        public override Task<LoginResponse> LoginWithToken(TokenRequest request, ServerCallContext context)
        {
            if (_authService.GetUserFromToken(request.Token) is User user)
            {
                return Task.FromResult(new LoginResponse()
                {
                    ExpirationDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow.AddHours(_appConfig.JwtExpireHours)),
                    Token = request.Token,
                    User = user
                });
            }

            return Task.FromResult(new LoginResponse());
        }

        [Authorize]
        public override Task<UserData> GetData(Empty request, ServerCallContext context)
        {
            return Task.FromResult(new UserData() { User = _authService.User });
        }

        [Authorize]
        public override async Task<Response> UserChange(User request, ServerCallContext context)
        {
            if (request.UserId == _authService.User.UserId)
            {
                return new Response() { ErrorType = await _appData.UserChangeAsync(request, false) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> UserChangePassword(User request, ServerCallContext context)
        {
            if (request.UserId == _authService.User.UserId)
            {
                return new Response() { ErrorType = await _appData.UserChangePasswordAsync(request, false) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }
    }
}
