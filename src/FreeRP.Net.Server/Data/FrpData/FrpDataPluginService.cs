using FreeRP.Net.Server.Database;
using System.Collections.Concurrent;

namespace FreeRP.Net.Server.Data
{
    public partial class FrpDataService : IFrpDataService
    {
        private readonly ConcurrentDictionary<string, GrpcService.Plugin.Plugin> _plugins = new();
        private readonly ConcurrentDictionary<string, GrpcService.Plugin.PluginRole> _pluginRoles = new();

        public IEnumerable<GrpcService.Plugin.Plugin> PluginsGetAll() => _plugins.Select(x => x.Value);
        public IEnumerable<GrpcService.Plugin.PluginRole> PluginRolesGetAll() => _pluginRoles.Select(x => x.Value);

        private void LoadPlugins()
        {
            try
            {
                var plugins = _db.Records.Where(x => x.RecordType == FrpSettings.RecordTypePlugin);
                if (plugins.Any())
                {
                    foreach (var plug in plugins)
                    {
                        if (Helpers.ProtoJson.GetModel<GrpcService.Plugin.Plugin>(plug.DataAsJson) is GrpcService.Plugin.Plugin p)
                        {
                            _plugins[plug.RecordId] = p;
                        }
                    }
                }

                var rpu = _db.Records.Where(x => x.RecordType == FrpSettings.RecordTypePluginRole);
                foreach (var r in rpu)
                {
                    if (Helpers.ProtoJson.GetModel<GrpcService.Plugin.PluginRole>(r.DataAsJson) is GrpcService.Plugin.PluginRole pr)
                    {
                        _pluginRoles[r.RecordId] = pr;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Can not load plugins");
            }
        }

        public GrpcService.Core.Response PluginInstall(string filePath)
        {
            try
            {
                //var zipDict = Path.Combine(_frpSettings.TempRootPath, Path.GetRandomFileName());
                //if (Directory.Exists(zipDict))
                //    Directory.Delete(zipDict, true);

                //Directory.CreateDirectory(zipDict);
                //System.IO.Compression.ZipFile.ExtractToDirectory(filePath, zipDict);

                //var pluginJsonFile = Path.Combine(zipDict, FrpSettings.PluginJsonFileName);
                //if (File.Exists(pluginJsonFile))
                //{
                //    var json = File.ReadAllText(pluginJsonFile);
                //    if (Helpers.ProtoJson.GetModel<GrpcService.Plugin.Plugin>(json) is GrpcService.Plugin.Plugin plug)
                //    {
                //        var pluginFolder = Path.Combine(_frpSettings.PluginRootPath, plug.PluginId);

                //        if (_plugins.TryGetValue(plug.PluginId, out GrpcService.Plugin.Plugin? p))
                //        {
                //            return new GrpcService.Core.Response() { ErrorType = GrpcService.Core.ErrorType.ErrorExist };
                //        }
                //        else
                //        {
                //            Directory.Move(zipDict, pluginFolder);
                //            _plugins[plug.PluginId] = plug;
                //        }

                //        var indexHtml = Path.Combine(pluginFolder, "index.html");
                //        if (File.Exists(indexHtml))
                //        {
                //            var html = File.ReadAllText(indexHtml);
                //            var htmlBase = RegexBaseHtml();
                //            if (htmlBase.IsMatch(html))
                //            {
                //                html = htmlBase.Replace(html, $"<base href=\"/{_frpSettings.PluginRootPath}/{plug.PluginId}/\" />");
                //                File.WriteAllText(indexHtml, html);
                //            }
                //        }

                //        return new() { ErrorType = GrpcService.Core.ErrorType.ErrorNone, Data = json };
                //    }
                //}
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Can not install or update plugin");
            }

            return new GrpcService.Core.Response() { ErrorType = GrpcService.Core.ErrorType.ErrorUnknown };
        }

        public GrpcService.Core.ErrorType PluginRoleAdd(GrpcService.Plugin.PluginRole role)
        {
            try
            {
                string id = role.PluginId + role.RoleId;
                if (_pluginRoles.ContainsKey(id) == false && _pluginRoles.TryAdd(id, role))
                {
                    var r = RecordContext.GetRecord(_frpSettings.AdminId, id, FrpSettings.RecordTypePluginRole, role.PluginId, Helpers.ProtoJson.GetJson(role));
                    _db.Records.Add(r);
                    _db.SaveChanges();

                    return GrpcService.Core.ErrorType.ErrorNone;
                }
                else
                {
                    return GrpcService.Core.ErrorType.ErrorExist;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "add role to plugin");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }

        public GrpcService.Core.ErrorType PluginRoleDelete(GrpcService.Plugin.PluginRole role)
        {
            try
            {
                string id = role.PluginId + role.RoleId;
                if (_pluginRoles.ContainsKey(id))
                {
                    //var r = _db.Records.FirstOrDefault(x => x.RecordId == id && x.RecordType == RecordTypePluginRole);
                    //if (r != null)
                    //{
                    //    _pluginRoles.TryRemove(id, out _);
                    //    var old = RecordContext.GetRecordRemove(r);
                    //    _db.Records.Remove(r);
                    //    _db.RecordsRemove.Add(old);
                    //    _db.SaveChanges();
                    //}

                    return GrpcService.Core.ErrorType.ErrorNone;
                }
                else
                {
                    return GrpcService.Core.ErrorType.ErrorNotExist;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "add role");
                return GrpcService.Core.ErrorType.ErrorUnknown;
            }
        }
    }
}
