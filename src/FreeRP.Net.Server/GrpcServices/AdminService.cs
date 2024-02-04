using FreeRP.GrpcService.Admin;
using FreeRP.GrpcService.Core;
using FreeRP.GrpcService.Database;
using FreeRP.GrpcService.Plugin;
using FreeRP.Net.Server.Data;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.Net.Server.GrpcServices
{
    public class AdminService
        (FrpSettings acs, IFrpDataService appData, AuthService authService) : GrpcService.Admin.AdminService.AdminServiceBase
    {
        private readonly FrpSettings _frpSettings = acs;
        private readonly IFrpDataService _appData = appData;
        private readonly AuthService _authService = authService;

        public override Task<LoginResponse> Login(User request, ServerCallContext context)
        {
            if (_frpSettings.Admin == request.Email && _frpSettings.AdminPassword == request.Password)
            {
                return Task.FromResult(new LoginResponse()
                {
                    ExpirationDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow.AddHours(_frpSettings.JwtExpireHours)),
                    Token = _authService.GenerateJwtToken(request, true),
                    User = new () { FirstName = "FreeRP", LastName = "Admin", Theme = new Theme() { BaseLayerLuminance = 0, AccentBaseColor = "#1E90FF" } }
                });
            }

            return Task.FromResult(new LoginResponse());
        }

        #region DataAccess

        [Authorize]
        public override async Task<Response> DataAccessCreate(DataAccess request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                return new Response() { ErrorType = await _appData.DataAccessAddAsync(request), Data = Helpers.ProtoJson.GetJson(request) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> DataAccessChange(DataAccess request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                return new Response() { ErrorType = await _appData.DataAccessChangeAsync(request) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> DataAccessDelete(DataAccess request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                return new Response() { ErrorType = await _appData.DataAccessDeleteAsync(request) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        #endregion

        #region User

        [Authorize]
        public override Task<UsersResponse> UsersGet(Empty request, ServerCallContext context)
        {
            var res = new UsersResponse();

            if (_authService.IsAdmin)
            {
                res.Users.AddRange(_appData.UserGetAll());
                return Task.FromResult(res);
            }

            res.ErrorType = ErrorType.ErrorAccessDenied;
            return Task.FromResult(res);
        }

        [Authorize]
        public override async Task<Response> UserCreate(User request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                if (request.IsApi)
                {
                    var dt = new DateTime(request.Ticks);
                    if (dt <= DateTime.UtcNow)
                        dt = DateTime.UtcNow.AddDays(1);

                    request.Password = _authService.GenerateJwtToken(request, dt);
                }

                var et = await _appData.UserAddAsync(request);
                if (et == ErrorType.ErrorNone)
                {
                    request.Password = "";
                    return new Response() { ErrorType = et, Data = Helpers.ProtoJson.GetJson(request) };
                }
                return new Response() { ErrorType = et };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> UserChange(User request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                if (request.IsApi)
                {
                    var dt = new DateTime(request.Ticks);
                    if (dt <= DateTime.UtcNow)
                        dt = DateTime.UtcNow.AddDays(1);

                    request.Password = _authService.GenerateJwtToken(request, dt);
                }

                return new Response() { ErrorType = await _appData.UserChangeAsync(request, true) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> UserChangePassword(User request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                return new Response() { ErrorType = await _appData.UserChangePasswordAsync(request, true) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> UserDelete(User request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                return new Response() { ErrorType = await _appData.UserDeleteAsync(request) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        #endregion

        #region Role

        public override Task<RolesResponse> RolesGet(Empty request, ServerCallContext context)
        {
            var res = new RolesResponse();

            if (_authService.IsAdmin)
            {
                res.Roles.AddRange(_appData.RolesGetAll());
                res.UserInRoles.AddRange(_appData.UserInRoleGetAll());
                return Task.FromResult(res);
            }

            res.ErrorType = ErrorType.ErrorAccessDenied;
            return Task.FromResult(res);
        }

        [Authorize]
        public override async Task<Response> RoleCreate(Role request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                var et = await _appData.RoleAddAsync(request);
                if (et == ErrorType.ErrorNone)
                {
                    return new Response() { ErrorType = et, Data = Helpers.ProtoJson.GetJson(request) };
                }
                return new Response() { ErrorType = et };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> RoleChange(Role request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                return new Response() { ErrorType = await _appData.RoleChangeAsync(request) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> RoleDelete(Role request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                return new Response() { ErrorType = await _appData.RoleDeleteAsync(request) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> RoleAddUser(UserInRole request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                var et = await _appData.RoleAddUserAsync(request);
                if (et == ErrorType.ErrorNone)
                {
                    return new Response() { ErrorType = et, Data = Helpers.ProtoJson.GetJson(request) };
                }
                return new Response() { ErrorType = et };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> RoleDeleteUser(UserInRole request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                return new Response() { ErrorType = await _appData.RoleDeleteUserAsync(request) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        #endregion

        #region Plugin

        //[Authorize]
        //public override async Task<Response> IntallPlugin(InstallPluginRequest request, ServerCallContext context)
        //{
        //    if (_authService.IsAdmin)
        //    {
        //        if (request.IsFile == false && request.Path.ToLower().StartsWith("http"))
        //        {
        //            try
        //            {
        //                var client = _httpClientFactory.CreateClient();
        //                var res = await client.GetStreamAsync(request.Path);

        //                var file = Path.GetRandomFileName();
        //                var path = Path.Combine(_appData.TempFolderPath, file);
        //                await using var writeStream = File.Create(path);
        //                await res.CopyToAsync(writeStream);

        //                request.IsFile = true;
        //                request.Path = path;
        //            }
        //            catch (Exception)
        //            {
        //                return new Response() { ErrorType = ErrorType.ErrorDownloadFile };
        //            }
        //        }
        //        else
        //        {
        //            request.Path = Path.Combine(_appData.TempFolderPath, request.Path);
        //        }

        //        return await Task.Run(() => _appData.PluginInstall(request.Path));
        //    }

        //    return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        //}

        [Authorize]
        public override Task<Response> PluginRoleAdd(PluginRole request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                return Task.FromResult(new Response() { ErrorType = _appData.PluginRoleAdd(request) });
            }

            return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorAccessDenied });
        }

        [Authorize]
        public override Task<Response> PluginRoleDelete(PluginRole request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                return Task.FromResult(new Response() { ErrorType = _appData.PluginRoleDelete(request) });
            }

            return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorAccessDenied });
        }

        #endregion
    }
}
