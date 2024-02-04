using FreeRP.Net.Client.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Services
{
    public class DatabaseService(ConnectService connectService, I18nService i18n)
    {
        private readonly ConnectService _connectService = connectService;
        private readonly I18nService _i18n = i18n;

        public async ValueTask<GrpcService.Database.Database?> GetDatabaseAsync(string databaseId)
        {
            var res = await _connectService.DatabaseServiceClient.DatabaseGetAsync(new GrpcService.Database.DataRequest() { DatabaseId = databaseId });
            switch (res.ErrorType)
            {
                case GrpcService.Core.ErrorType.ErrorNone:
                    {
                        if (Helpers.GrpcJson.GetModel<GrpcService.Database.Database>(res.Data) is GrpcService.Database.Database db)
                        {
                            db.ConnectService = _connectService;
                            db.I18n = _i18n;
                            return db;
                        }
                        goto case GrpcService.Core.ErrorType.ErrorUnknown;
                    }
                case GrpcService.Core.ErrorType.ErrorAccessDenied:
                    throw new UnauthorizedAccessException();
                case GrpcService.Core.ErrorType.ErrorDatabaseNotExist:
                    throw new KeyNotFoundException();
                case GrpcService.Core.ErrorType.ErrorUnknown:
                    throw new Exception($"{_i18n.Text.UnknownError}: {res.Message}");
                default:
                    break;
            }
            return null;
        }

        public async ValueTask<string?> DatabaseAddAsync(GrpcService.Database.Database database)
        {
            try
            {
                var res = await _connectService.DatabaseServiceClient.DatabaseCreateAsync(database, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    case GrpcService.Core.ErrorType.ErrorDatabaseId:
                        return _i18n.Text.XIsRequired.Replace("{0}", _i18n.Text.DatabaseId);
                    case GrpcService.Core.ErrorType.ErrorDatabaseTableId:
                        return _i18n.Text.XIsRequired.Replace("{0}", _i18n.Text.DatabaseTableId);
                    case GrpcService.Core.ErrorType.ErrorDatabaseFieldId:
                        return _i18n.Text.XIsRequired.Replace("{0}", _i18n.Text.DatabaseFieldId);
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

        public async ValueTask<string?> DatabaseChangeAsync(GrpcService.Database.Database database)
        {
            try
            {
                var res = await _connectService.DatabaseServiceClient.DatabaseChangeAsync(database, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
                    case GrpcService.Core.ErrorType.ErrorUnknown:
                        return _i18n.Text.UnknownError;
                    case GrpcService.Core.ErrorType.ErrorAccessDenied:
                        return _i18n.Text.AccessDenied;
                    case GrpcService.Core.ErrorType.ErrorDatabaseTableId:
                        return _i18n.Text.XIsRequired.Replace("{0}", _i18n.Text.DatabaseTableId);
                    case GrpcService.Core.ErrorType.ErrorDatabaseFieldId:
                        return _i18n.Text.XIsRequired.Replace("{0}", _i18n.Text.DatabaseFieldId);
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

        public async ValueTask<string?> DatabaseDeleteAsync(GrpcService.Database.Database database)
        {
            try
            {
                var res = await _connectService.DatabaseServiceClient.DatabaseDeleteAsync(database, headers: _connectService.AuthHeader);
                switch (res.ErrorType)
                {
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
    }
}
