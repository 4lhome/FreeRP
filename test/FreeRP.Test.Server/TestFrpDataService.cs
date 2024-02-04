using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Test.Server
{
    [TestClass]
    public class TestFrpDataService
    {
        public GrpcService.Core.User User { get; set; } = new();
        public GrpcService.Core.Role Role { get; set; } = new();
        public GrpcService.Core.UserInRole UserInRole { get; set; } = new();
        public GrpcService.Core.DataAccess DataAccessFile { get; set; } = new() { DataAccessId = "file://" };
        public GrpcService.Core.DataAccess DataAccessDb { get; set; } = new() { DataAccessId = "db://" };
        public GrpcService.Core.DataAccess DataAccessPublic { get; set; } = new() { DataAccessId = "public://" };
        public GrpcService.Core.DataAccess DataAccessTemp { get; set; } = new() { DataAccessId = "temp://" };
        public GrpcService.Content.ContentUriRequest TestDirFile { get; set; } = new() { Uri = $"file:///{nameof(TestDirFile)}" };
        public GrpcService.Content.ContentUriRequest TestDirDb { get; set; } = new() { Uri = $"db:///{nameof(TestDirDb)}" };
        public GrpcService.Content.ContentUriRequest TestDirPublic { get; set; } = new() { Uri = $"public:///{nameof(TestDirPublic)}" };
        public GrpcService.Content.ContentUriRequest TestDirTemp { get; set; } = new() { Uri = $"temp:///{nameof(TestDirTemp)}" };

        public GrpcService.Database.Database Database { get; set; } = new() { DatabaseId = "test", Change = true };

        private readonly string _password = "TestPass123!";
        private readonly Net.Server.Data.FrpSettings _frpSettings;
        private readonly Net.Server.Data.FrpDataService _frpDataService;
        private readonly Net.Server.Data.AuthService _authService;

        public TestFrpDataService()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Net.Server.Data.FrpSettings.DataFolderName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            _frpSettings = Net.Server.Data.FrpSettings.Create(AppDomain.CurrentDomain.BaseDirectory);

            _frpDataService = new(new Microsoft.Extensions.Logging.Abstractions.NullLogger<Net.Server.Data.FrpDataService>(),
            _frpSettings, new Net.Server.Data.UriService(_frpSettings));

            _authService = new(_frpDataService, _frpSettings);
            _authService.Roles.Add(Role);
        }

        [TestMethod]
        public async Task TestFull()
        {
            await UserCreateAsync();
            await UserChangeAsync();
            await UserChangPasswordAsync();

            await RoleAddAsync();
            await RoleChangeAsync();
            await RoleAddUserAsync();

            await DataAccessAddAsync();
            await DataAccessChangeAsync();

            await DirectoryCreateAsync();
            await DirectoryChangeAsync();
            await DirectoryReadAsync();
            await DirectoryDeleteAsync();

            //Clean
            await RoleDeleteUserAsync();
            await DataAccessDeleteAsync();
            await UserDeleteAsync();
            await RoleDeleteAsync();
        }

        #region User

        public async Task UserCreateAsync()
        {
            User = new()
            {
                Email = "foobar",
            };

            var res = await _frpDataService.UserAddAsync(User);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorEmailInvalid);
            User.Email = $"{Path.GetRandomFileName()}@test.test";

            string[] passList = ["1", "aaaaaaaa", "1aaaaaaa", "1AAAAAAA", "1aAaaaaa" ];
            for (int i = 0; i < passList.Length; i++)
            {
                User.Password = passList[i];
                res = await _frpDataService.UserAddAsync(User);

                if (i == 0) Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorPasswordToShort);
                else if (i == 1) Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorPasswordNumber);
                else if (i == 2) Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorPasswordUpperChar);
                else if (i == 3) Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorPasswordLowerChar);
                else if (i == 4) Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorPasswordSymbols);
            }

            User.Password = _password;
            res = await _frpDataService.UserAddAsync(User);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
            Assert.IsFalse(string.IsNullOrEmpty(User.UserId));

            res = await _frpDataService.UserAddAsync(User);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorExist);
        }

        public async Task UserChangeAsync()
        {
            var u = User.Clone();
            u.Email = "test";

            var res = await _frpDataService.UserChangeAsync(u, true);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorEmailInvalid);

            u.UserId = "test";
            res = await _frpDataService.UserChangeAsync(u, true);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNotExist);
        }

        public async Task UserChangPasswordAsync()
        {
            var u = User.Clone();
            u.Password = "test";

            var res = await _frpDataService.UserChangePasswordAsync(u, true);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorPasswordToShort);

            u.Password = _password;
            res = await _frpDataService.UserChangePasswordAsync(u, true);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);

            Assert.IsTrue(string.IsNullOrEmpty(u.Password));
        }

        public async Task UserDeleteAsync()
        {
            var u = User.Clone();
            u.UserId = "test";
            var res = await _frpDataService.UserChangeAsync(u, true);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNotExist);

            res = await _frpDataService.UserDeleteAsync(User);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
        }

        #endregion

        #region Role

        public async Task RoleAddAsync()
        {
            Role.Name = Path.GetRandomFileName();
            var res = await _frpDataService.RoleAddAsync(Role);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);

            var r = Role.Clone();
            r.RoleId = "";
            res = await _frpDataService.RoleAddAsync(r);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorExist);
        }

        public async Task RoleChangeAsync()
        {
            var r = Role.Clone();
            r.RoleId = "";

            var res = await _frpDataService.RoleChangeAsync(r);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorExist);

            r.Name = "test";
            res = await _frpDataService.RoleChangeAsync(r);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNotExist);

            Role.Name = Path.GetRandomFileName();
            res = await _frpDataService.RoleChangeAsync(Role);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
        }

        public async Task RoleAddUserAsync()
        {
            UserInRole = new() { RoleId = "test", UserId = "test" };

            var res = await _frpDataService.RoleAddUserAsync(UserInRole);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNotExist);

            UserInRole.RoleId = Role.RoleId;
            UserInRole.UserId = User.UserId;

            res = await _frpDataService.RoleAddUserAsync(UserInRole);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
        }

        public async Task RoleDeleteAsync()
        {
            var r = Role.Clone();
            r.RoleId = "";

            var res = await _frpDataService.RoleDeleteAsync(r);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNotExist);

            res = await _frpDataService.RoleDeleteAsync(Role);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
        }

        public async Task RoleDeleteUserAsync()
        {
            var uir = UserInRole.Clone();
            uir.UserInRoleId = "test";

            var res = await _frpDataService.RoleDeleteUserAsync(uir);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNotExist);

            res = await _frpDataService.RoleDeleteUserAsync(UserInRole);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
        }

        #endregion

        #region Data access

        public async Task DataAccessAddAsync()
        {
            var res = await _frpDataService.DataAccessAddAsync(DataAccessFile);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorDataAccessRoleNotExist);

            var da = DataAccessFile.Clone();
            da.RoleId = Role.RoleId;
            da.DataAccessId = "test";
            res = await _frpDataService.DataAccessAddAsync(da);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorUriSchemeNotSupported);

            DataAccessFile.RoleId = Role.RoleId;
            res = await _frpDataService.DataAccessAddAsync(DataAccessFile);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
            Assert.IsTrue(DataAccessFile.DataAccessId == "file:///");

            res = await _frpDataService.DataAccessAddAsync(DataAccessFile);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorExist);

            DataAccessDb.RoleId = Role.RoleId;
            res = await _frpDataService.DataAccessAddAsync(DataAccessDb);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
            Assert.IsTrue(DataAccessDb.DataAccessId == "db:///");

            DataAccessPublic.RoleId = Role.RoleId;
            res = await _frpDataService.DataAccessAddAsync(DataAccessPublic);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
            Assert.IsTrue(DataAccessPublic.DataAccessId == "public:///");

            DataAccessTemp.RoleId = Role.RoleId;
            res = await _frpDataService.DataAccessAddAsync(DataAccessTemp);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
            Assert.IsTrue(DataAccessTemp.DataAccessId == "temp:///");
        }

        public async Task DataAccessChangeAsync()
        {
            var da = DataAccessFile.Clone();
            da.RoleId = "";
            da.DataAccessId = "";

            var res = await _frpDataService.DataAccessChangeAsync(da);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
            Assert.IsTrue(da.RoleId == DataAccessFile.RoleId && da.DataAccessId == DataAccessFile.DataAccessId);

            res = await _frpDataService.DataAccessChangeAsync(DataAccessFile);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);

            res = await _frpDataService.DataAccessChangeAsync(DataAccessDb);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);

            res = await _frpDataService.DataAccessChangeAsync(DataAccessPublic);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);

            res = await _frpDataService.DataAccessChangeAsync(DataAccessTemp);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
        }

        public async Task DataAccessDeleteAsync()
        {
            var da = DataAccessFile.Clone();
            da.RoleDataAccessId = "test";
            var res = await _frpDataService.DataAccessDeleteAsync(da);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNotExist);

            res = await _frpDataService.DataAccessDeleteAsync(DataAccessFile);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);

            res = await _frpDataService.DataAccessDeleteAsync(DataAccessDb);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);

            res = await _frpDataService.DataAccessDeleteAsync(DataAccessPublic);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);

            res = await _frpDataService.DataAccessDeleteAsync(DataAccessTemp);
            Assert.IsTrue(res == GrpcService.Core.ErrorType.ErrorNone);
        }

        #endregion

        #region Directory

        public async Task DirectoryCreateAsync()
        {
            //File
            var res = await _frpDataService.DirectoryCreateAsync(TestDirFile, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorAccessDenied);

            DataAccessFile.Create = true;
            await DataAccessChangeAsync();
            res = await _frpDataService.DirectoryCreateAsync(TestDirFile, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorNone);
            Assert.IsTrue(Directory.Exists(Path.Combine(_frpSettings.ContentRootPath, nameof(TestDirFile))));

            //Db
            res = await _frpDataService.DirectoryCreateAsync(TestDirDb, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorAccessDenied);

            DataAccessDb.Create = true;
            await DataAccessChangeAsync();
            res = await _frpDataService.DirectoryCreateAsync(TestDirDb, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorNone);
            Assert.IsTrue(Directory.Exists(Path.Combine(_frpSettings.DatabasesRootPath, nameof(TestDirDb))));

            //Public
            res = await _frpDataService.DirectoryCreateAsync(TestDirPublic, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorAccessDenied);

            DataAccessPublic.Create = true;
            await DataAccessChangeAsync();
            res = await _frpDataService.DirectoryCreateAsync(TestDirPublic, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorNone);
            Assert.IsTrue(Directory.Exists(Path.Combine(_frpSettings.PublicRootPath, nameof(TestDirPublic))));

            //Temp
            res = await _frpDataService.DirectoryCreateAsync(TestDirTemp, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorAccessDenied);

            DataAccessTemp.Create = true;
            await DataAccessChangeAsync();
            res = await _frpDataService.DirectoryCreateAsync(TestDirTemp, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorNone);
            Assert.IsTrue(Directory.Exists(Path.Combine(_frpSettings.TempRootPath, nameof(TestDirTemp))));
        }

        public async Task DirectoryChangeAsync()
        {
            GrpcService.Content.ChangeContentUriRequest change = new()
            {
                SourceUri = TestDirFile.Uri,
                DestUri = TestDirFile.Uri + 2
            };
            var res = await _frpDataService.DirectoryPathChangeAsync(change, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorAccessDenied);

            DataAccessFile.Change = true;
            await DataAccessChangeAsync();
            res = await _frpDataService.DirectoryPathChangeAsync(change, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorNone);

            TestDirFile.Uri = change.DestUri;
            Assert.IsTrue(Directory.Exists(Path.Combine(_frpSettings.ContentRootPath, nameof(TestDirFile) + 2)));
        }

        public async Task DirectoryReadAsync()
        {
            var res = await _frpDataService.GetContentTreeAsync(TestDirFile, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorAccessDenied);

            DataAccessFile.Read = true;
            await DataAccessChangeAsync();
            res = await _frpDataService.GetContentTreeAsync(TestDirFile, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorNone);
        }

        public async Task DirectoryDeleteAsync()
        {
            var res = await _frpDataService.DirectoryDeleteAsync(TestDirFile, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorAccessDenied);

            DataAccessFile.Delete = true;
            await DataAccessChangeAsync();
            res = await _frpDataService.DirectoryDeleteAsync(TestDirFile, _authService);
            Assert.IsTrue(res.ErrorType == GrpcService.Core.ErrorType.ErrorNone);
        }

        #endregion
    }
}
