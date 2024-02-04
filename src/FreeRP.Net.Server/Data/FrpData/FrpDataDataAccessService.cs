using FreeRP.Net.Server.Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace FreeRP.Net.Server.Data
{
    public partial class FrpDataService : IFrpDataService
    {
        private readonly ConcurrentDictionary<string, GrpcService.Core.DataAccess> _dataAccess = new();

        private void LoadDataAccess()
        {
            try
            {
                var das = _db.Records.Where(x => x.RecordType == FrpSettings.RecordTypeDataAccess);
                if (das.Any())
                {
                    foreach (var da in das)
                    {
                        if (Helpers.ProtoJson.GetModel<GrpcService.Core.DataAccess>(da.DataAsJson) is GrpcService.Core.DataAccess data)
                        {
                            _dataAccess[data.RoleDataAccessId] = data;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Can not load data access");
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> DataAccessAddAsync(GrpcService.Core.DataAccess data)
        {
            if (_roles.ContainsKey(data.RoleId) == false)
                return GrpcService.Core.ErrorType.ErrorDataAccessRoleNotExist;

            var uri = _uriService.GetUri(data.DataAccessId);
            if (uri is null)
                return GrpcService.Core.ErrorType.ErrorUriSchemeNotSupported;

            data.DataAccessId = uri.OriginalString;
            data.RoleDataAccessId = data.RoleId + data.DataAccessId;
            if (_dataAccess.ContainsKey(data.RoleDataAccessId))
                return GrpcService.Core.ErrorType.ErrorExist;

            await _db.Records.AddAsync(RecordContext.GetRecord(_frpSettings.AdminId, data.RoleDataAccessId, FrpSettings.RecordTypeDataAccess, _frpSettings.AdminId, Helpers.ProtoJson.GetJson(data)));
            await _db.SaveChangesAsync();
            _dataAccess[data.RoleDataAccessId] = data;

            return GrpcService.Core.ErrorType.ErrorNone;
        }

        public async ValueTask<GrpcService.Core.ErrorType> DataAccessChangeAsync(GrpcService.Core.DataAccess data)
        {
            if (_dataAccess.TryGetValue(data.RoleDataAccessId, out var da))
            {
                data.DataAccessId = da.DataAccessId;
                data.RoleId = da.RoleId;

                var old = await _db.Records.FirstOrDefaultAsync(x => x.RecordId == da.RoleDataAccessId && x.RecordType == FrpSettings.RecordTypeDataAccess);
                if (old is not null)
                {
                    _db.Records.Remove(old);

                    await _db.Records.AddAsync(RecordContext.GetRecord(_frpSettings.AdminId, data.RoleDataAccessId, FrpSettings.RecordTypeDataAccess, old.Owner, Helpers.ProtoJson.GetJson(data)));
                    await _db.RecordChangeds.AddAsync(RecordContext.GetRecordChanged(_frpSettings.AdminId, false, Helpers.ProtoJson.GetJson(old)));
                    await _db.SaveChangesAsync();

                    _dataAccess[data.RoleDataAccessId] = data;
                    return GrpcService.Core.ErrorType.ErrorNone;
                }
            }

            return GrpcService.Core.ErrorType.ErrorNotExist;
        }

        public async ValueTask<GrpcService.Core.ErrorType> DataAccessDeleteAsync(GrpcService.Core.DataAccess data)
        {
            if (_dataAccess.TryRemove(data.RoleDataAccessId, out _))
            {
                var old = await _db.Records.FirstOrDefaultAsync(x => x.RecordId == data.RoleDataAccessId && x.RecordType == FrpSettings.RecordTypeDataAccess);
                if (old is not null)
                {
                    _db.Records.Remove(old);
                    await _db.RecordChangeds.AddAsync(RecordContext.GetRecordChanged(_frpSettings.AdminId, true, Helpers.ProtoJson.GetJson(old)));
                    await _db.SaveChangesAsync();

                    return GrpcService.Core.ErrorType.ErrorNone;
                }
            }

            return GrpcService.Core.ErrorType.ErrorNotExist;
        }

        public bool DataAccessAllowRead(IEnumerable<GrpcService.Core.Role> roles, Uri uri)
        {
            foreach (var item in roles)
            {
                var da = GetDataAccess(item, uri);
                if (da.Read)
                    return true;
            }

            return false;
        }

        public bool DataAccessAllowCreate(IEnumerable<GrpcService.Core.Role> roles, Uri uri)
        {
            foreach (var item in roles)
            {
                var da = GetDataAccess(item, uri);
                if (da.Create)
                    return true;
            }

            return false;
        }

        public bool DataAccessAllowDelete(IEnumerable<GrpcService.Core.Role> roles, Uri uri)
        {
            foreach (var item in roles)
            {
                var da = GetDataAccess(item, uri);
                if (da.Delete)
                    return true;
            }

            return false;
        }

        public bool DataAccessAllowChange(IEnumerable<GrpcService.Core.Role> roles, Uri uri)
        {
            foreach (var item in roles)
            {
                var da = GetDataAccess(item, uri);
                if (da.Change)
                    return true;
            }

            return false;
        }

        public GrpcService.Core.DataAccess GetDataAccess(GrpcService.Core.Role role, Uri uri)
        {
            GrpcService.Core.DataAccess access = new();
            string path = uri.AbsoluteUri;

            if (uri.Segments.Length > 0)
            {
                for (int i = uri.Segments.Length; i > 0; i--)
                {
                    if (_dataAccess.TryGetValue($"{role.RoleId}{path}", out GrpcService.Core.DataAccess? da))
                    {
                        access = da;
                        break;
                    }

                    path = path.Replace(uri.Segments[i - 1], "");
                }
            }

            return access;
        }
    }
}
