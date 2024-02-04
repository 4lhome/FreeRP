using FreeRP.Net.Client.Translation;
using Microsoft.FluentUI.AspNetCore.Components.DesignTokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Services
{
    public class UserService
    {
        
        public GrpcService.User.UserData UserData { get; set; } = new();

        private readonly ConnectService _connectService;
        private readonly I18nService _i18n;

        public bool IsLogin { get; set; }

        public UserService(ConnectService connectService, I18nService i18n)
        {
            _connectService = connectService;
            _i18n = i18n;
        }

        public async Task<bool> LoginAsync(GrpcService.Core.User user)
        {
            try
            {
                var res = await _connectService.UserServiceClient.LoginAsync(user);
                if (res != null && string.IsNullOrEmpty(res.Token) == false)
                {
                    _connectService.LoginResponse.MergeFrom(res);
                    _connectService.AuthHeader.Clear();
                    _connectService.AuthHeader.Add("Authorization", $"Bearer {res.Token}");

                    await SetSessionAsync();
                    IsLogin = true;
                    return true;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        public async Task<bool> LoginWithToken(string token)
        {
            try
            {
                var res = await _connectService.UserServiceClient.LoginWithTokenAsync(new GrpcService.User.TokenRequest() { Token = token });
                if (res != null && string.IsNullOrEmpty(res.Token) == false)
                {
                    _connectService.LoginResponse.MergeFrom(res);
                    _connectService.AuthHeader.Clear();
                    _connectService.AuthHeader.Add("Authorization", $"Bearer {res.Token}");

                    await SetSessionAsync();
                    IsLogin = true;
                    return true;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        public async Task SetSessionAsync()
        {
            await Task.Delay(500);
        }

        public async Task RemoveSessionAsync()
        {
            await Task.Delay(500);
        }

        public async Task<bool> LoadFromSessonAsync()
        {
            await Task.Delay(500);
            return false;
        }

        public async Task LoadDataAsync()
        {
            UserData = await _connectService.UserServiceClient.GetDataAsync(new(), headers: _connectService.AuthHeader);
        }

        public async Task<string?> UserChangeAsync(GrpcService.Core.User user)
        {
            try
            {
                var res = await _connectService.UserServiceClient.UserChangeAsync(user, headers: _connectService.AuthHeader);
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

        public async Task<string?> UserChangePasswordAsync()
        {
            try
            {
                var res = await _connectService.UserServiceClient.UserChangePasswordAsync(UserData.User, headers: _connectService.AuthHeader);
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

                UserData.User.Password = "";
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
