using FreeRP.Net.Server.Database;
using FreeRP.Net.Server.GrpcServices;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace FreeRP.Net.Server.Data
{
    public sealed partial class FrpDataService : IFrpDataService
    {
        private readonly FrpSettings _frpSettings;
        private readonly RecordContext _db;
        private readonly ILogger<FrpDataService> _log;
        private readonly UriService _uriService;

        public GrpcService.Database.Database Database { get; set; }

        public FrpDataService(ILogger<FrpDataService> log, FrpSettings frpSettings, UriService uriService)
        {
            Database = new()
            {
                DatabaseId = FrpSettings.FrpDatabaseName,
                Name = FrpSettings.FrpDatabaseName,
                DatabaseProvider = GrpcService.Database.DatabaseProvider.Sqlite
            };

            _frpSettings = frpSettings;
            _db = new(_frpSettings, Database);
            _db.Database.EnsureCreated();

            _log = log;
            _uriService = uriService;

            LoadAll();
        }

        private void LoadAll()
        {
            LoadUsers();
            LoadRoles();
            LoadDataAccess();
            LoadDatabases();
            LoadPlugins();
        }

        #region Helpers

        private static string GetGuidId<T>(ConcurrentDictionary<string, T> d)
        {
            while (true)
            {
                string g = Guid.NewGuid().ToString();
                if (d.ContainsKey(g) == false)
                    return g;
            }
        }

        private string GetPasswordHash(string password)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: _frpSettings.PasswordSalt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));
        }

        private static bool IsEmailValid(string email)
        {
            return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
