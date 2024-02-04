using FreeRP.GrpcService.Core;
using FreeRP.GrpcService.Database;
using FreeRP.Net.Server.Data;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace FreeRP.Net.Server.GrpcServices
{
    public class DatabaseService(FrpSettings frpSettings, IFrpDataService appData, AuthService authService) : GrpcService.Database.DatabaseService.DatabaseServiceBase
    {
        private readonly FrpSettings _frpSettings = frpSettings;
        private readonly IFrpDataService _appData = appData;
        private readonly AuthService _authService = authService;

        //public override Task<QueryResponse> DataQuery(QueryRequest request, ServerCallContext context)
        //{
        //    if (_appData.DatabaseGet(request.DatabaseId) is GrpcService.Database.Database database)
        //    {
        //        Database.RecordContext db = new(_frpSettings, database);

        //    }


        //    return base.DataQuery(request, context);
        //}

        [Authorize]
        public override async Task<Response> DatabaseOpen(GrpcService.Database.Database request, ServerCallContext context)
        {
            var db = _appData.DatabaseGetWithAccess(request.DatabaseId, _authService.Roles, _authService.IsAdmin);
            if (db is not null)
            {
                if (_authService.IsAdmin)
                    return new Response() { ErrorType = await _appData.DatabaseOpenAsync(request, _frpSettings.AdminId) };

                if (db.Change == false && db.Create == false && db.Delete == false && db.Read == false)
                    return new Response() { ErrorType = ErrorType.ErrorAccessDenied };

                return new Response() { ErrorType = await _appData.DatabaseOpenAsync(request, _authService.User.UserId) };
            }

            return new Response() { ErrorType = ErrorType.ErrorNotExist };
        }

        [Authorize]
        public override async Task<Response> DatabaseItemAdd(DataRequest request, ServerCallContext context)
        {
            var db = _appData.DatabaseGetWithAccess(request.DatabaseId, _authService.Roles, _authService.IsAdmin);
            if (db is not null)
            {

            }

            return new Response() { ErrorType = ErrorType.ErrorNotExist };
        }

        [Authorize]
        public override Task<Response> DatabaseItemUpdate(DataRequest request, ServerCallContext context)
        {
            var db = _appData.DatabaseGetWithAccess(request.DatabaseId, _authService.Roles, _authService.IsAdmin);
            if (db is not null)
            {

            }

            return base.DatabaseItemUpdate(request, context);
        }

        [Authorize]
        public override Task<Response> DatabaseItemRemove(DataRequest request, ServerCallContext context)
        {
            var db = _appData.DatabaseGetWithAccess(request.DatabaseId, _authService.Roles, _authService.IsAdmin);
            if (db is not null)
            {

            }

            return base.DatabaseItemRemove(request, context);
        }

        [Authorize]
        public override Task<QueryResponse> DatabaseItemQuery(QueryRequest request, ServerCallContext context)
        {
            var db = _appData.DatabaseGetWithAccess(request.DatabaseId, _authService.Roles, _authService.IsAdmin);
            if (db is not null)
            {

            }

            return base.DatabaseItemQuery(request, context);
        }

        [Authorize]
        public override Task<Response> DatabaseGet(DataRequest request, ServerCallContext context)
        {
            var db = _appData.DatabaseGetWithAccess(request.DatabaseId, _authService.Roles, _authService.IsAdmin);
            if (db is not null)
            {
                if (db.Change == false && db.Create == false && db.Delete == false && db.Read == false)
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorAccessDenied });
                else
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorNone, Data = Helpers.ProtoJson.GetJson(db) });
            }
            else
            {
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorDatabaseNotExist });
            }
        }

        [Authorize]
        public override async Task<Response> DatabaseCreate(GrpcService.Database.Database request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                var et = await _appData.DatabaseAdd(request, _frpSettings.AdminId);
                if (et == ErrorType.ErrorNone)
                {
                    return new Response() { ErrorType = et, Data = Helpers.ProtoJson.GetJson(request) };
                }
                return new Response() { ErrorType = et };
            }
            else if (_authService.User.IsDeveloper)
            {
                var et = await _appData.DatabaseAdd(request, _authService.User.UserId);
                if (et == ErrorType.ErrorNone)
                {
                    return new Response() { ErrorType = et, Data = Helpers.ProtoJson.GetJson(request) };
                }
                return new Response() { ErrorType = et };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> DatabaseChange(GrpcService.Database.Database request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                var et = await _appData.DatabaseChangeAsync(request, _frpSettings.AdminId);
                if (et == ErrorType.ErrorNone)
                {
                    return new Response() { ErrorType = et, Data = Helpers.ProtoJson.GetJson(request) };
                }
                return new Response() { ErrorType = et };
            }
            else if (_authService.User.IsDeveloper)
            {
                var et = await _appData.DatabaseChangeAsync(request, _authService.User.UserId);
                if (et == ErrorType.ErrorNone)
                {
                    return new Response() { ErrorType = et, Data = Helpers.ProtoJson.GetJson(request) };
                }
                return new Response() { ErrorType = et };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }

        [Authorize]
        public override async Task<Response> DatabaseDelete(GrpcService.Database.Database request, ServerCallContext context)
        {
            if (_authService.IsAdmin)
            {
                return new Response() { ErrorType = await _appData.DatabaseDeleteAsync(request, _frpSettings.AdminId) };
            }
            else if (_authService.User.IsDeveloper)
            {
                return new Response() { ErrorType = await _appData.DatabaseDeleteAsync(request, _authService.User.UserId) };
            }

            return new Response() { ErrorType = ErrorType.ErrorAccessDenied };
        }
    }
}
