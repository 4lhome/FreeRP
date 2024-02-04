using FreeRP.Net.Client.Translation;
using FreeRP.Net.Client.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using FreeRP.Net.Client.Data;
using System.Net.Http;

namespace FreeRP.Net.Client.Translation
{
    public class I18nService
    {
        public I18n Text { get; set; } = new();
        
        public delegate void I18tEventHandler();
        public event I18tEventHandler? TextChanged;
        public static readonly string[] Languages = ["en", "de"];

        public I18nService() 
        {
            SetText("en"); 
        }

        public Task LoadTextAsync(string code)
        {
            if (code.Contains('-'))
                code = code.Split('-')[0];

            if (Languages.Contains(code) == false)
            {
                code = "en";
            }

            SetText(code);

            return Task.CompletedTask;

            //var json = await _httpClient.GetStringAsync("/_content/FreeRP.Net.Client/lang/lang.json");
            //var languages = Helpers.Json.GetModel<string[]>(json);

            //if (languages is not null)
            //{
            //    if (languages.Contains(code) == false)
            //    {
            //        code = "en";
            //    }

            //    var res = await _httpClient.GetStringAsync($"/_content/FreeRP.Net.Client/lang/{code}.json");
            //    if (res != null && Helpers.Json.GetModel<I18n>(res) is I18n i18n)
            //    {
            //        Text = i18n;
            //        TextChanged?.Invoke();
            //    }
            //}
        }

        private void SetText(string code)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"FreeRP.Net.Client.Translation.Lang.{code}.json";
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using StreamReader reader = new(stream);
                var json = reader.ReadToEnd();
                var txt = Helpers.Json.GetModel<I18n>(json);
                if (txt is not null)
                {
                    Text = txt;
                    TextChanged?.Invoke();
                }
            }
        }

        public string GetErrorText(GrpcService.Core.ErrorType errorType, string replace = "")
        {
            switch (errorType)
            {
                case GrpcService.Core.ErrorType.ErrorUnknown:
                    return Text.UnknownError;
                case GrpcService.Core.ErrorType.ErrorAccessDenied:
                    return Text.AccessDenied;
                case GrpcService.Core.ErrorType.ErrorNotExist:
                    return Text.XNotExist.Replace("{0}", replace);
                case GrpcService.Core.ErrorType.ErrorExist:
                    return Text.XExist.Replace("{0}", replace);
                case GrpcService.Core.ErrorType.ErrorDownloadFile:
                    break;
                case GrpcService.Core.ErrorType.ErrorId:
                    break;
                case GrpcService.Core.ErrorType.ErrorPasswordToShort:
                    break;
                case GrpcService.Core.ErrorType.ErrorPasswordNumber:
                    break;
                case GrpcService.Core.ErrorType.ErrorPasswordUpperChar:
                    break;
                case GrpcService.Core.ErrorType.ErrorPasswordLowerChar:
                    break;
                case GrpcService.Core.ErrorType.ErrorPasswordSymbols:
                    break;
                case GrpcService.Core.ErrorType.ErrorDatabaseId:
                    break;
                case GrpcService.Core.ErrorType.ErrorDatabaseTableId:
                    break;
                case GrpcService.Core.ErrorType.ErrorDatabaseFieldId:
                    break;
                case GrpcService.Core.ErrorType.ErrorDatabaseNotExist:
                    break;
                case GrpcService.Core.ErrorType.ErrorUriSchemeNotSupported:
                    break;
                case GrpcService.Core.ErrorType.ErrorFileExist:
                    break;
                case GrpcService.Core.ErrorType.ErrorFileNotExist:
                    break;
                case GrpcService.Core.ErrorType.ErrorDirectoryExist:
                    break;
                case GrpcService.Core.ErrorType.ErrorDirectoryNotExist:
                    break;
                case GrpcService.Core.ErrorType.ErrorDataAccessRoleNotExist:
                    break;
                default:
                    return string.Empty;
            }

            return string.Empty;
        }
    }
}
