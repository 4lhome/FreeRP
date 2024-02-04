using FreeRP.Net.Server.Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace FreeRP.Net.Server.Data
{
    public partial class FrpDataService : IFrpDataService
    {
        private readonly ConcurrentDictionary<string, GrpcService.Core.Role> _roles = new();
        private readonly ConcurrentDictionary<string, GrpcService.Core.UserInRole> _userInRoles = new();

        public IEnumerable<GrpcService.Core.Role> RolesGetAll() => _roles.Select(x => x.Value).OrderBy(x => x.Name);
        public IEnumerable<GrpcService.Core.UserInRole> UserInRoleGetAll() => _userInRoles.Select(x => x.Value);
        public GrpcService.Core.Role? RoleGet(string roleId)
        {
            if (_roles.TryGetValue(roleId, out GrpcService.Core.Role? role))
                return role;

            return null;
        }

        private void LoadRoles()
        {
            try
            {
                var roles = _db.Records.Where(x => x.RecordType == FrpSettings.RecordTypeRole);
                if (roles.Any())
                {
                    foreach (var r in roles)
                    {
                        if (Helpers.ProtoJson.GetModel<GrpcService.Core.Role>(r.DataAsJson) is GrpcService.Core.Role role)
                        {
                            _roles[r.RecordId] = role;
                        }
                    }
                }

                var uirs = _db.Records.Where(x => x.RecordType == FrpSettings.RecordTypeUserInRole);
                if (uirs.Any())
                {
                    foreach (var item in uirs)
                    {
                        if (Helpers.ProtoJson.GetModel<GrpcService.Core.UserInRole>(item.DataAsJson) is GrpcService.Core.UserInRole uir)
                        {
                            _userInRoles[item.RecordId] = uir;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Can not load roles");
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> RoleAddAsync(GrpcService.Core.Role role)
        {
            try
            {
                var ava = _roles.Where(x => x.Value.Name.Equals(role.Name, StringComparison.CurrentCultureIgnoreCase));
                if (ava.Any() == false)
                {
                    role.RoleId = GetGuidId(_roles);
                    var r = RecordContext.GetRecord(_frpSettings.AdminId, role.RoleId, FrpSettings.RecordTypeRole, _frpSettings.AdminId, Helpers.ProtoJson.GetJson(role));
                    if (_roles.TryAdd(role.RoleId, role))
                    {
                        await _db.Records.AddAsync(r);
                        await _db.SaveChangesAsync();

                        return GrpcService.Core.ErrorType.ErrorNone;
                    }

                    throw new Exception("Can not add role");
                }
                else
                {
                    return GrpcService.Core.ErrorType.ErrorExist;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "add role");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> RoleAddUserAsync(GrpcService.Core.UserInRole userInRole)
        {
            try
            {
                if (_userInRoles.Select(x => x.Value).FirstOrDefault(x => x.UserId == userInRole.UserId && x.RoleId == userInRole.RoleId) != null)
                {
                    return GrpcService.Core.ErrorType.ErrorNone;
                }

                if (_roles.ContainsKey(userInRole.RoleId) && _users.ContainsKey(userInRole.UserId))
                {
                    userInRole.UserInRoleId = GetGuidId(_userInRoles);
                    if (_userInRoles.TryAdd(userInRole.UserInRoleId, userInRole))
                    {
                        await _db.Records.AddAsync(RecordContext.GetRecord(_frpSettings.AdminId, userInRole.UserInRoleId, FrpSettings.RecordTypeUserInRole, userInRole.RoleId, Helpers.ProtoJson.GetJson(userInRole)));
                        await _db.SaveChangesAsync();

                        return GrpcService.Core.ErrorType.ErrorNone;
                    }

                    throw new Exception("Can not add user to role");
                }

                return GrpcService.Core.ErrorType.ErrorNotExist;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "add user to role");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> RoleChangeAsync(GrpcService.Core.Role role)
        {
            try
            {
                var e = _roles.Values.FirstOrDefault(x => x.Name.Equals(role.Name, StringComparison.CurrentCultureIgnoreCase));
                if (e != null && e.RoleId != role.RoleId)
                {
                    return GrpcService.Core.ErrorType.ErrorExist;
                }

                if (_roles.ContainsKey(role.RoleId))
                {
                    var re = _db.Records.First(x => x.RecordId == role.RoleId && x.RecordType == FrpSettings.RecordTypeRole);
                    _db.Records.Remove(re);

                    await _db.RecordChangeds.AddAsync(RecordContext.GetRecordChanged(_frpSettings.AdminId, false, Helpers.Json.GetJson(re)));
                    await _db.Records.AddAsync(RecordContext.GetRecord(_frpSettings.AdminId, role.RoleId, FrpSettings.RecordTypeRole, re.Owner, Helpers.ProtoJson.GetJson(role)));
                    await _db.SaveChangesAsync();

                    _roles[role.RoleId] = role;

                    return GrpcService.Core.ErrorType.ErrorNone;
                }

                return GrpcService.Core.ErrorType.ErrorNotExist;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "update role");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> RoleDeleteAsync(GrpcService.Core.Role role)
        {
            try
            {
                if (_roles.TryRemove(role.RoleId, out GrpcService.Core.Role? value))
                {
                    var dbr = await _db.Records.FirstAsync(x => x.RecordId == role.RoleId && x.RecordType == FrpSettings.RecordTypeRole);
                    _db.Records.Remove(dbr);

                    List<GrpcService.Database.Record> records = [];
                    records.Add(dbr);

                    var uirs = _userInRoles.Where(x => x.Value.RoleId == role.RoleId).ToArray();
                    foreach (var item in uirs)
                    {
                        _userInRoles.TryRemove(item);
                        var dbru = await _db.Records.FirstOrDefaultAsync(x => x.RecordId == item.Key);
                        if (dbru is not null)
                        {
                            _db.Records.Remove(dbru);
                            records.Add(dbru);
                        }
                    }

                    var ads = _dataAccess.Where(x => x.Key.StartsWith(role.RoleId)).ToList();
                    if (ads.Count > 0)
                    {
                        foreach (var ad in ads)
                        {
                            var rm = await _db.Records.FirstOrDefaultAsync(x => x.RecordId == ad.Value.RoleDataAccessId && x.RecordType == FrpSettings.RecordTypeDataAccess);
                            if (rm is not null)
                            {
                                _db.Records.Remove(rm);
                                records.Add(rm);
                            }
                            _dataAccess.TryRemove(ad);
                        }
                    }

                    await _db.RecordChangeds.AddAsync(RecordContext.GetRecordChanged(_frpSettings.AdminId, true, Helpers.Json.GetJson(records)));
                    await _db.SaveChangesAsync();
                    return GrpcService.Core.ErrorType.ErrorNone;
                }

                return GrpcService.Core.ErrorType.ErrorNotExist;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "delete role");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> RoleDeleteUserAsync(GrpcService.Core.UserInRole userInRole)
        {
            try
            {
                if (_userInRoles.TryRemove(userInRole.UserInRoleId, out _))
                {
                    var dbr = await _db.Records.FirstAsync(x => x.RecordType == FrpSettings.RecordTypeUserInRole && x.RecordId == userInRole.UserInRoleId);
                    _db.Records.Remove(dbr);
                    await _db.RecordChangeds.AddAsync(RecordContext.GetRecordChanged(_frpSettings.AdminId, true, Helpers.Json.GetJson(dbr)));
                    await _db.SaveChangesAsync();

                    return GrpcService.Core.ErrorType.ErrorNone;
                }

                return GrpcService.Core.ErrorType.ErrorNotExist;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "delete user from role");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }
    }
}
