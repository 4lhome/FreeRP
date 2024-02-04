using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreeRP.Net.Server.Data
{
    public class FrpSettings
    {
        public const string SettingsFileName = "frpsettings.json";

        public string ServerName { get; set; } = "FreeRP";
        public string IconUrl { get; set; } = "/icon.svg";

        //Content
        public const string DataFolderName = "frpdata";
        public const string TempFolderName = "tmp";
        public const string ContentFolderName = "directory";
        public const string PublicFolderName = "public";
        public string AppRoot { get; set; } = string.Empty;
        public string FrpRootPath { get; set; } = string.Empty;
        public string TempRootPath { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public string PublicRootPath { get; set; } = string.Empty;

        //Plugin
        public const string PluginFolderName = "plug";
        public const string PluginJsonFileName = "plugin.json";
        public string PluginRootPath { get; set; } = string.Empty;

        //Database
        public const string FrpDatabaseName = "freeRPAppDb";
        public const string DatabasesFolderName = "db";
        public string DatabasesRootPath { get; set; } = string.Empty;

        public const string RecordTypeUser = "User";
        public const string RecordTypeUserInRole = "UserInRole";
        public const string RecordTypeDatabase = "Databases";
        public const string RecordTypeRole = "Role";
        public const string RecordTypeDataAccess = "DataAccess";
        public const string RecordTypePlugin = "Plugin";
        public const string RecordTypePluginRole = "PluginRole";
        public const string RecordTypePluginFunctionRoleRights = "PluginFunctionRoleRights";
        
        //Auth
        public bool WithPassword { get; set; } = true;
        public int PasswordLength { get; set; } = 8;
        public byte[] PasswordSalt { get; set; } = Guid.NewGuid().ToByteArray();
        public string JwtKey { get; set; } = Guid.NewGuid().ToString().Replace("-", "");
        public int JwtExpireHours { get; set; } = 24;

        //Admin
        public string Admin { get; set; } = "admin";
        public string AdminPassword { get; set; } = "admin";
        public string AdminId { get; set; } = "0";

        //Grpc
        public int GrpcMessageSize { get; set; } = 4 * 1024 * 1024; //4 MB
        public int GrpcTransportDeadlineMinutes { get; set; } = 5;

        public static string GetSettingsSavePath(string rootPath) =>
            Path.Combine(rootPath, DataFolderName, SettingsFileName);

        public static FrpSettings Create(string appRoot)
        {
            var frpRoot = Path.Combine(appRoot, DataFolderName);

            var settings = new FrpSettings()
            {
                AppRoot = appRoot,
                FrpRootPath = frpRoot,
                PublicRootPath = Path.Combine(frpRoot, PublicFolderName),
                TempRootPath = Path.Combine(frpRoot, TempFolderName),
                DatabasesRootPath = Path.Combine(frpRoot, DatabasesFolderName),
                ContentRootPath = Path.Combine(frpRoot, ContentFolderName),
                PluginRootPath = Path.Combine(frpRoot, PluginFolderName),
            };

            if (Directory.Exists(settings.FrpRootPath) is false)
                Directory.CreateDirectory(settings.FrpRootPath);

            if (Directory.Exists(settings.PublicRootPath) is false)
                Directory.CreateDirectory(settings.PublicRootPath);

            if (Directory.Exists(settings.TempRootPath))
                Directory.CreateDirectory(settings.TempRootPath);

            if (Directory.Exists(settings.DatabasesRootPath) is false)
                Directory.CreateDirectory(settings.DatabasesRootPath);

            if (Directory.Exists(settings.ContentRootPath) is false)
                Directory.CreateDirectory(settings.ContentRootPath);

            if (Directory.Exists(settings.PluginRootPath) is false)
                Directory.CreateDirectory(settings.PluginRootPath);

            Save(settings);
            return settings;
        }

        public static void Save(FrpSettings settings)
        {
            JsonSerializerOptions jo = new()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };

           var json = JsonSerializer.Serialize(settings, jo);
           File.WriteAllText(GetSettingsSavePath(settings.AppRoot), json);
        }
    }
}
