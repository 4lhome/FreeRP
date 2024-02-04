using FreeRP.Net.Server.Database;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FreeRP.Net.Server.Data
{
    public partial class FrpDataService : IFrpDataService
    {
        public readonly ConcurrentDictionary<string, RecordContext> OpenDatabases = [];

        
        private readonly ConcurrentDictionary<string, GrpcService.Database.Database> _databases = new();
        public IEnumerable<GrpcService.Database.Database> DatabaseGetAll() => _databases.Values;
        public GrpcService.Database.Database? DatabaseGet(string databaseId)
        {
            if (_databases.TryGetValue(databaseId, out GrpcService.Database.Database? db))
                return db;

            return null;
        }

        private void LoadDatabases()
        {
            try
            {
                var dbs = _db.Records.Where(x => x.RecordType == FrpSettings.RecordTypeDatabase);
                if (dbs.Any())
                {
                    foreach (var r in dbs)
                    {
                        if (Helpers.ProtoJson.GetModel<GrpcService.Database.Database>(r.DataAsJson) is GrpcService.Database.Database database)
                        {
                            _databases[r.RecordId] = database;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Can not load databases");
            }
        }

        #region Database

        public GrpcService.Database.Database? DatabaseGetWithAccess(string databaseId, IEnumerable<GrpcService.Core.Role> roles, bool isAdmin)
        {
            var db = DatabaseGet(databaseId);
            if (db is not null)
            {
                db = db.Clone();
                foreach (var r in roles)
                {
                    string id;
                    GrpcService.Core.DataAccess? da;
                    foreach (var t in db.Tables)
                    {
                        foreach (var f in t.Fields)
                        {
                            id = $"db:///{r.RoleId}{databaseId}/{t.TableId}/{f.FieldId}";
                            if (_dataAccess.TryGetValue(id, out da))
                            {
                                if (da.Change) f.Change = t.Change = db.Change = true;
                                if (da.Create) f.Create = t.Create = db.Create = true;
                                if (da.Delete) f.Delete = t.Delete = db.Delete = true;
                                if (da.Read) f.Read = t.Read = db.Read = true;
                            }
                        }

                        if (t.Change && t.Create && t.Delete && t.Read)
                            continue;
                        else
                        {
                            id = $"db:///{r.RoleId}{databaseId}/{t.TableId}";
                            if (_dataAccess.TryGetValue(id, out da))
                            {
                                if (da.Change) t.Change = db.Change = true;
                                if (da.Create) t.Create = db.Create = true;
                                if (da.Delete) t.Delete = db.Delete = true;
                                if (da.Read) t.Read = db.Read = true;
                            }
                        }
                    }

                    if (db.Change && db.Create && db.Delete && db.Read)
                        continue;
                    else
                    {
                        id = $"db:///{r.RoleId}{databaseId}";
                        if (_dataAccess.TryGetValue(id, out da))
                        {
                            if (da.Change) db.Change = true;
                            if (da.Create) db.Create = true;
                            if (da.Delete) db.Delete = true;
                            if (da.Read) db.Read = true;
                        }
                    }
                }
            }

            return null;
        }

        public async ValueTask<GrpcService.Core.ErrorType> DatabaseAdd(GrpcService.Database.Database database, string id)
        {
            try
            {
                if (string.IsNullOrEmpty(database.DatabaseId))
                {
                    return GrpcService.Core.ErrorType.ErrorDatabaseId;
                }
                else if (_databases.ContainsKey(database.DatabaseId))
                {
                    return GrpcService.Core.ErrorType.ErrorExist;
                }

                var et = DatabaseCheckConfig(database);
                if (et == GrpcService.Core.ErrorType.ErrorNone)
                {
                    if (_databases.TryAdd(database.DatabaseId, database))
                    {
                        _db.Records.Add(RecordContext.GetRecord(id, database.DatabaseId, FrpSettings.RecordTypeDatabase, id, Helpers.ProtoJson.GetJson(database)));
                        await _db.SaveChangesAsync();

                        var context = new RecordContext(_frpSettings, database);
                        await context.Database.EnsureCreatedAsync();
                        await context.DisposeAsync();

                        return GrpcService.Core.ErrorType.ErrorNone;
                    }
                    else
                        throw new Exception("Can not add database");
                }
                else
                    return et;

            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Add new database");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> DatabaseChangeAsync(GrpcService.Database.Database ndb, string id)
        {
            try
            {
                if (
                    _databases.TryGetValue(ndb.DatabaseId, out GrpcService.Database.Database? odb) &&
                    _db.Records.FirstOrDefault(x => x.RecordId == odb.DatabaseId && x.RecordType == FrpSettings.RecordTypeDatabase) is GrpcService.Database.Record old)
                {
                    var et = DatabaseCheckConfig(ndb);
                    if (et == GrpcService.Core.ErrorType.ErrorNone)
                    {
                        _db.Records.Remove(old);
                        _db.RecordChangeds.Add(RecordContext.GetRecordChanged(id, false, Helpers.Json.GetJson(old)));
                        _db.Records.Add(RecordContext.GetRecord(id, ndb.DatabaseId, FrpSettings.RecordTypeDatabase, old.Owner, Helpers.ProtoJson.GetJson(ndb)));
                        await _db.SaveChangesAsync();
                        _databases[ndb.DatabaseId] = ndb;

                        return GrpcService.Core.ErrorType.ErrorNone;
                    }
                    else
                        return et;
                }
                else
                {
                    return GrpcService.Core.ErrorType.ErrorNotExist;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "update database");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> DatabaseDeleteAsync(GrpcService.Database.Database ndb, string id)
        {
            try
            {
                if (_databases.TryRemove(ndb.DatabaseId, out GrpcService.Database.Database? odb) && odb is not null)
                {
                    var old = _db.Records.FirstOrDefault(x => x.RecordId == odb.DatabaseId && x.RecordType == FrpSettings.RecordTypeDatabase);
                    if (old != null)
                    {
                        _db.Records.Remove(old);
                        _db.RecordChangeds.Add(RecordContext.GetRecordChanged(id, true, Helpers.Json.GetJson(old)));
                        await _db.SaveChangesAsync();
                    }

                    return GrpcService.Core.ErrorType.ErrorNone;
                }
                else
                {
                    return GrpcService.Core.ErrorType.ErrorNotExist;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "delete database");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        private static GrpcService.Core.ErrorType DatabaseCheckConfig(GrpcService.Database.Database db)
        {
            if (string.IsNullOrEmpty(db.DatabaseId))
                return GrpcService.Core.ErrorType.ErrorDatabaseId;

            db.Change = false;
            db.Create = false;
            db.Delete = false;
            db.Read = false;

            if (db.Tables.Count > 0)
            {
                foreach (var t in db.Tables)
                {
                    if (string.IsNullOrEmpty(t.TableId))
                        return GrpcService.Core.ErrorType.ErrorDatabaseTableId;

                    if (db.Tables.Where(x => x.TableId == t.TableId).Count() > 1)
                        return GrpcService.Core.ErrorType.ErrorDatabaseSameTableId;

                    t.Change = false;
                    t.Create = false;
                    t.Delete = false;
                    t.Read = false;

                    if (t.Fields.Count > 0)
                    {
                        if (t.Fields.FirstOrDefault(x => x.IsId) is null)
                            return GrpcService.Core.ErrorType.ErrorDatabaseFieldIdNotExist;

                        foreach (var f in t.Fields)
                        {
                            if (string.IsNullOrEmpty(f.FieldId))
                                return GrpcService.Core.ErrorType.ErrorDatabaseFieldId;

                            if (t.Fields.Where(x => x.FieldId == f.FieldId).Count() > 1)
                                return GrpcService.Core.ErrorType.ErrorDatabaseSameFieldId;

                            f.Change = false;
                            f.Create = false;
                            f.Delete = false;
                            f.Read = false;
                        }
                    }
                }
            }

            return GrpcService.Core.ErrorType.ErrorNone;
        }

        #endregion

        #region Database item

        public ValueTask<GrpcService.Core.ErrorType> DatabaseOpenAsync(GrpcService.Database.Database database, string id)
        {
            var key = $"{id}{database.DatabaseId}";
            if (OpenDatabases.ContainsKey(key) == false)
            {
                OpenDatabases[key] = new RecordContext(_frpSettings, database);
                return ValueTask.FromResult(GrpcService.Core.ErrorType.ErrorNone);
            }

            return ValueTask.FromResult(GrpcService.Core.ErrorType.ErrorNone);
        }

        public async ValueTask<GrpcService.Core.ErrorType> DatabaseSaveChangesAsync(GrpcService.Database.Database database, string id)
        {
            try
            {
                var key = $"{id}{database.DatabaseId}";
                if (OpenDatabases.TryRemove(key, out var db))
                {
                    await db.SaveChangesAsync();
                    return GrpcService.Core.ErrorType.ErrorNone;
                }
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "save open database");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> DatabaseItemAddAsync(GrpcService.Database.DataRequest dataRequest, GrpcService.Database.Database database)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dataRequest.TableId))
                {
                    return GrpcService.Core.ErrorType.ErrorDatabaseTableId;
                }

                var dt = database.Tables.FirstOrDefault(x => x.TableId == dataRequest.TableId);
                if (dt is null)
                {
                    if (database.Create == false)
                    {
                        return GrpcService.Core.ErrorType.ErrorAccessDenied;
                    }

                    if (database.AllowUnknownTables == false)
                    {
                        return GrpcService.Core.ErrorType.ErrorDatabaseTableNotExist;
                    }

                    

                    
                }
                
                

                return GrpcService.Core.ErrorType.ErrorNone;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Add item to database");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> DatabaseItemUpdateAsync(GrpcService.Database.DataRequest dataRequest, GrpcService.Database.Database database)
        {
            try
            {
                return GrpcService.Core.ErrorType.ErrorNone;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Update item in database");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> DatabaseItemRemoveAsync(GrpcService.Database.DataRequest dataRequest, GrpcService.Database.Database database)
        {
            try
            {
                return GrpcService.Core.ErrorType.ErrorNone;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Remove item from database");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public async ValueTask<GrpcService.Core.ErrorType> DatabaseItemQueryAsync(GrpcService.Database.QueryRequest queryRequest, GrpcService.Database.Database database)
        {
            try
            {
                return GrpcService.Core.ErrorType.ErrorNone;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Get item query from database");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        #endregion
    }
}
