using FreeRP.Net.Client.Translation;
using Microsoft.FluentUI.AspNetCore.Components.DesignTokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Services
{
    public class AdminService(ConnectService connectService, I18nService i18n)
    {
        private readonly ConnectService _connectService = connectService;
        private readonly I18nService _i18n = i18n;
        
        public bool IsLogin { get; set; }

        public async Task<bool> LoginAsync(GrpcService.Core.User user)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.LoginAsync(user);
                if (res != null && string.IsNullOrEmpty(res.Token) == false)
                {
                    _connectService.LoginResponse.MergeFrom(res);
                    _connectService.AuthHeader.Clear();
                    _connectService.AuthHeader.Add("Authorization", $"Bearer {res.Token}");

                    IsLogin = true;
                    return true;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        #region DataAccess

        public async ValueTask<string?> DataAccessCreateAsync(GrpcService.Core.DataAccess data)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.DataAccessCreateAsync(data, _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    case GrpcService.Core.ErrorType.ErrorDataAccessRoleNotExist:
                        return _i18n.Text.XNotExist.Replace("{0}", _i18n.Text.Role);
                    case GrpcService.Core.ErrorType.ErrorUriSchemeNotSupported:
                        return _i18n.Text.UriSchemeNotSupported;
                    case GrpcService.Core.ErrorType.ErrorExist:
                        return _i18n.Text.XExist.Replace("{0}", _i18n.Text.DataAccess);
                    default:
                        var da = Helpers.GrpcJson.GetModel<GrpcService.Core.DataAccess>(res.Data);
                        if (da is not null)
                        {
                            data.RoleDataAccessId = da.RoleDataAccessId;
                        }
                        return null;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async ValueTask<string?> DataAccessChangeAsync(GrpcService.Core.DataAccess data)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.DataAccessChangeAsync(data, _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    case GrpcService.Core.ErrorType.ErrorNotExist:
                        return _i18n.Text.XNotExist.Replace("{0}", _i18n.Text.DataAccess);
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async ValueTask<string?> DataAccessDeleteAsync(GrpcService.Core.DataAccess data)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.DataAccessDeleteAsync(data, _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    case GrpcService.Core.ErrorType.ErrorNotExist:
                        return _i18n.Text.XNotExist.Replace("{0}", _i18n.Text.DataAccess);
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion

        #region User

        public async Task<GrpcService.Admin.UsersResponse> UsersGetAsync()
        {
            return await _connectService.AdminServiceClient.UsersGetAsync(new GrpcService.Core.Empty(), _connectService.AuthHeader);
        }

        public async Task<string?> UserAddAsync(GrpcService.Core.User user)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.UserCreateAsync(user, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    case GrpcService.Core.ErrorType.ErrorExist:
                        return _i18n.Text.UserExist;
                    case GrpcService.Core.ErrorType.ErrorPasswordToShort:
                    case GrpcService.Core.ErrorType.ErrorPasswordNumber:
                    case GrpcService.Core.ErrorType.ErrorPasswordUpperChar:
                    case GrpcService.Core.ErrorType.ErrorPasswordLowerChar:
                    case GrpcService.Core.ErrorType.ErrorPasswordSymbols:
                        return _i18n.Text.PasswordValidateError.Replace("{0}", _connectService.ConnectResponse.PasswordLength.ToString());
                    default:
                        break;
                }

                if (Helpers.GrpcJson.GetModel<GrpcService.Core.User>(res.Data) is GrpcService.Core.User u)
                {
                    user.MergeFrom(u);
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string?> UserChangeAsync(GrpcService.Core.User user)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.UserChangeAsync(user, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorNotExist:
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    default:
                        break;
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string?> UserChangePasswordAsync(GrpcService.Core.User user)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.UserChangePasswordAsync(user, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorNotExist:
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    case GrpcService.Core.ErrorType.ErrorPasswordToShort:
                    case GrpcService.Core.ErrorType.ErrorPasswordNumber:
                    case GrpcService.Core.ErrorType.ErrorPasswordUpperChar:
                    case GrpcService.Core.ErrorType.ErrorPasswordLowerChar:
                    case GrpcService.Core.ErrorType.ErrorPasswordSymbols:
                        return _i18n.Text.PasswordValidateError.Replace("{0}", _connectService.ConnectResponse.PasswordLength.ToString());
                    default:
                        break;
                }

                user.Password = "";
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string?> UserDeleteAsync(GrpcService.Core.User user)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.UserDeleteAsync(user, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorNotExist:
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    default:
                        break;
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion

        #region Role

        public async Task<GrpcService.Admin.RolesResponse> RolesGetAsync()
        {
            return await _connectService.AdminServiceClient.RolesGetAsync(new GrpcService.Core.Empty(), _connectService.AuthHeader);
        }

        public async Task<string?> RoleAddAsync(GrpcService.Core.Role role)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.RoleCreateAsync(role, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    case GrpcService.Core.ErrorType.ErrorExist:
                        return _i18n.Text.RoleExist;
                    default:
                        break;
                }

                if (Helpers.GrpcJson.GetModel<GrpcService.Core.Role>(res.Data) is GrpcService.Core.Role r)
                {
                    role.MergeFrom(r);
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string?> RoleChangeAsync(GrpcService.Core.Role role)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.RoleChangeAsync(role, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorNotExist:
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    default:
                        break;
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string?> RoleDeleteAsync(GrpcService.Core.Role role)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.RoleDeleteAsync(role, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorNotExist:
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    default:
                        break;
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string?> RoleAddUserAsync(GrpcService.Core.UserInRole userInRole)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.RoleAddUserAsync(userInRole, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorNotExist:
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    default:
                        break;
                }

                if (Helpers.GrpcJson.GetModel<GrpcService.Core.UserInRole>(res.Data) is GrpcService.Core.UserInRole r)
                {
                    userInRole.MergeFrom(r);
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string?> RoleDeleteUserAsync(GrpcService.Core.UserInRole userInRole)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.RoleDeleteUserAsync(userInRole, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorNotExist:
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    default:
                        break;
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion

        #region Plugin

        public async Task<string?> InstallPluginAsync(GrpcService.Admin.InstallPluginRequest req)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.IntallPluginAsync(req, headers: _connectService.AuthHeader);
                if (res.ErrorType == GrpcService.Core.ErrorType.ErrorNone)
                {
                    return null;
                }

                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    case GrpcService.Core.ErrorType.ErrorDownloadFile:
                        return _i18n.Text.DownloadFileError;
                    default:
                        break;
                }

                return _i18n.Text.UnknownError;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string?> PluginRoleAddAsync(GrpcService.Plugin.PluginRole pr)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.PluginRoleAddAsync(pr, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    case GrpcService.Core.ErrorType.ErrorExist:
                        return _i18n.Text.RoleExist;
                    default:
                        break;
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string?> PluginRoleDeleteAsync(GrpcService.Plugin.PluginRole pr)
        {
            try
            {
                var res = await _connectService.AdminServiceClient.PluginRoleDeleteAsync(pr, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    case GrpcService.Core.ErrorType.ErrorExist:
                        return _i18n.Text.RoleExist;
                    default:
                        break;
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion
    }
}
