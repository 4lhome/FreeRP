using FreeRP.Net.Server.Database;
using FreeRP.Net.Server.GrpcServices;
using System.Collections.Concurrent;

namespace FreeRP.Net.Server.Data
{
    public partial class FrpDataService : IFrpDataService
    {
        private readonly ConcurrentDictionary<string, GrpcService.Core.User> _users = new();
        private readonly ConcurrentDictionary<string, string> _userPasswords = new();
        public GrpcService.Core.User UserGetByEmail(string name) => _users.FirstOrDefault(x => x.Value.Email == name).Value;
        public IEnumerable<GrpcService.Core.User> UserGetAll() => _users.Where(x => x.Value.IsApi == false).Select(x => x.Value);
        public IEnumerable<GrpcService.Core.User> UserApiGetAll() => _users.Where(x => x.Value.IsApi == true).Select(x => x.Value);


        private void LoadUsers()
        {
            try
            {
                var users = _db.Records.Where(x => x.RecordType == FrpSettings.RecordTypeUser);
                foreach (var item in users)
                {
                    if (Helpers.ProtoJson.GetModel<GrpcService.Core.User>(item.DataAsJson) is GrpcService.Core.User u)
                    {
                        _userPasswords[u.UserId] = u.Password;

                        if (u.IsApi == false)
                            u.Password = "";

                        _users[u.UserId] = u;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Load users from Database");
            }
        }

        public bool UserCheckPassword(GrpcService.Core.User user)
        {
            if (_userPasswords.TryGetValue(user.UserId, out string? p))
            {
                return p == GetPasswordHash(user.Password);
            }
            return false;
        }

        public IEnumerable<GrpcService.Core.Role> UserGetRoles(GrpcService.Core.User user)
        {
            try
            {
                return (from r in _roles
                        from ur in _userInRoles
                        where ur.Value.UserId == user.UserId
                        where r.Value.RoleId == ur.Value.RoleId
                        select r.Value).ToArray();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "get user roles");
                return new List<GrpcService.Core.Role>();
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> UserAddAsync(GrpcService.Core.User u)
        {
            try
            {
                u.Email = u.Email.ToLower();
                if (IsEmailValid(u.Email) == false)
                {
                    return GrpcService.Core.ErrorType.ErrorEmailInvalid;
                }

                if (u.IsApi == false)
                {
                    var passVal = AuthService.ValidatePassword(u.Password, _frpSettings.PasswordLength);
                    if (passVal != GrpcService.Core.ErrorType.ErrorNone)
                    {
                        return passVal;
                    }
                    u.Password = GetPasswordHash(u.Password);
                }

                if (UserGetByEmail(u.Email) != null)
                {
                    return GrpcService.Core.ErrorType.ErrorExist;
                }

                u.UserId = GetGuidId(_users);

                if (_users.TryAdd(u.UserId, u) && _userPasswords.TryAdd(u.UserId, u.Password))
                {
                    await _db.Records.AddAsync(RecordContext.GetRecord(_frpSettings.AdminId, u.UserId, FrpSettings.RecordTypeUser, u.UserId, Helpers.ProtoJson.GetJson(u)));
                    await _db.SaveChangesAsync();
                    return GrpcService.Core.ErrorType.ErrorNone;
                }
                else
                    throw new Exception("Can not add user");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Add new user");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> UserChangeAsync(GrpcService.Core.User user, bool admin)
        {
            try
            {
                if (_users.ContainsKey(user.UserId))
                {
                    user.Email = user.Email.ToLower();
                    if (IsEmailValid(user.Email) == false)
                    {
                        return GrpcService.Core.ErrorType.ErrorEmailInvalid;
                    }

                    var r = _db.Records.First(x => x.RecordId == user.UserId && x.RecordType == FrpSettings.RecordTypeUser);
                    _db.Records.Remove(r);

                    user.Password = _userPasswords[user.UserId];
                    string id = user.UserId;
                    if (admin)
                        id = _frpSettings.AdminId;

                    await _db.RecordChangeds.AddAsync(RecordContext.GetRecordChanged(id, false, Helpers.Json.GetJson(r)));
                    await _db.Records.AddAsync(RecordContext.GetRecord(id, user.UserId, FrpSettings.RecordTypeUser, r.Owner, Helpers.ProtoJson.GetJson(user)));

                    await _db.SaveChangesAsync();

                    if (user.IsApi == false)
                        user.Password = "";

                    _users[user.UserId] = user;

                    return GrpcService.Core.ErrorType.ErrorNone;
                }

                return GrpcService.Core.ErrorType.ErrorNotExist;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "update person");

                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> UserChangePasswordAsync(GrpcService.Core.User user, bool admin)
        {
            try
            {
                var passVal = AuthService.ValidatePassword(user.Password, _frpSettings.PasswordLength);
                if (passVal != GrpcService.Core.ErrorType.ErrorNone)
                {
                    return passVal;
                }

                if (_users.ContainsKey(user.UserId))
                {
                    var r = _db.Records.First(x => x.RecordId == user.UserId && x.RecordType == FrpSettings.RecordTypeUser);
                    _db.Records.Remove(r);

                    user.Password = GetPasswordHash(user.Password);
                    string id = user.UserId;
                    if (admin)
                        id = _frpSettings.AdminId;

                    await _db.RecordChangeds.AddAsync(RecordContext.GetRecordChanged(id, false, Helpers.Json.GetJson(r)));
                    await _db.Records.AddAsync(RecordContext.GetRecord(id, user.UserId, FrpSettings.RecordTypeUser, r.Owner, Helpers.ProtoJson.GetJson(user)));

                    await _db.SaveChangesAsync();

                    _userPasswords[user.UserId] = user.Password;
                    user.Password = "";

                    return GrpcService.Core.ErrorType.ErrorNone;
                }

                return GrpcService.Core.ErrorType.ErrorNotExist;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "user change password");

                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> UserDeleteAsync(GrpcService.Core.User u)
        {
            try
            {
                if (_users.ContainsKey(u.UserId))
                {
                    var r = _db.Records.First(x => x.RecordId == u.UserId && x.RecordType == FrpSettings.RecordTypeUser);
                    _db.Records.Remove(r);

                    List<GrpcService.Database.Record> records = [];
                    records.Add(r);

                    var uirs = _userInRoles.Where(x => x.Value.UserId == u.UserId).ToList();
                    foreach (var uir in uirs)
                    {
                        r = _db.Records.FirstOrDefault(x => x.RecordId == uir.Key);
                        if (r != null)
                        {
                            _db.Records.Remove(r);
                            records.Add(r);
                        }

                        _userInRoles.TryRemove(uir);
                    }

                    await _db.RecordChangeds.AddAsync(RecordContext.GetRecordChanged(_frpSettings.AdminId, true, Helpers.Json.GetJson(records)));
                    await _db.SaveChangesAsync();

                    _users.TryRemove(u.UserId, out _);
                    _userPasswords.TryRemove(u.UserId, out _);

                    return GrpcService.Core.ErrorType.ErrorNone;
                }

                return GrpcService.Core.ErrorType.ErrorNotExist;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "User delete");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }
    }
}
