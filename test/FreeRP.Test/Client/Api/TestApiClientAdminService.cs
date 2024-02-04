extern alias fx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Test.Client.Api
{
    public class TestApiClientAdminService
    {
        public fx::FreeRP.GrpcService.Core.User User { get; set; } = new();
        public fx::FreeRP.GrpcService.Core.Role Role { get; set; } = new();

        private readonly fx::FreeRP.GrpcService.Core.User _adminUser = new() { Email = "admin", Password = "admin" };
        private fx::FreeRP.GrpcService.Core.UserInRole _userInRole = new();
        private readonly fx::FreeRP.Net.Client.Services.AdminService _adminService = new(ApiClientTestSetting.ConnectService, ApiClientTestSetting.I18n);

        [TestMethod]
        public async Task TestFull()
        {
            Assert.IsTrue(await ApiClientTestSetting.ConnectToServerAsync(), "Connect to server");
            Assert.IsTrue(await LoginAsAdminAsync(), "Login as admin");

            await UserAddAsync();
            await UserChangeAsync();

            await RoleAddAsync();
            await RoleChangeAsync();

            await RoleAddUserAsync();

            fx::FreeRP.GrpcService.Core.DataAccess dataAccess = new()
            {
                DataAccessId = "file:///",
                RoleId = Role.RoleId
            };

            await DataAccessAddAsync(dataAccess);
            dataAccess.Change = true;
            await DataAccessChangeAsync(dataAccess);

            await UserDeleteAsync();
            await RoleDeleteAsync();
        }

        public async Task<bool> LoginAsAdminAsync()
        {
            return await _adminService.LoginAsync(_adminUser);
        }

        public async Task UserAddAsync()
        {
            string password = "TestPass123!";
            User = new()
            {
                Email = $"{Path.GetRandomFileName()}@test.test",
                Password = password,
                FirstName = "FirstName"
            };

            var res = await _adminService.UserAddAsync(User);
            Assert.IsNull(res);
            Assert.IsFalse(string.IsNullOrEmpty(User.UserId));

            var users = await _adminService.UsersGetAsync();
            Assert.IsNotNull(users.Users.FirstOrDefault(x => x.Email == User.Email));
            User.Password = password;
        }

        public async Task UserChangeAsync()
        {
            User.FirstName = Path.GetRandomFileName();
            await _adminService.UserChangeAsync(User);

            var users = await _adminService.UsersGetAsync();
            Assert.IsNotNull(users.Users.FirstOrDefault(x => x.Email == User.Email));

            User.Password = "ChangeTestPass123!";
            var res = await _adminService.UserChangePasswordAsync(User);
            Assert.IsNull(res);
        }

        public async Task UserDeleteAsync()
        {
            var res = await _adminService.UserDeleteAsync(User);
            Assert.IsNull(res);

            var users = await _adminService.UsersGetAsync();
            Assert.IsNull(users.Users.FirstOrDefault(x => x.Email == User.Email));
        }

        public async Task RoleAddAsync()
        {
            Role = new()
            {
                Name = Path.GetRandomFileName()
            };

            var res = await _adminService.RoleAddAsync(Role);
            Assert.IsNull(res);
            Assert.IsFalse(string.IsNullOrEmpty(Role.RoleId));
        }

        public async Task RoleChangeAsync()
        {
            Role.Name = Path.GetRandomFileName();
            var res = await _adminService.RoleChangeAsync(Role);
            Assert.IsNull(res);

            var roles = await _adminService.RolesGetAsync();
            Assert.IsNotNull(roles.Roles.FirstOrDefault(x => x.Name == Role.Name));
        }

        public async Task RoleDeleteAsync()
        {
            var res = await _adminService.RoleDeleteAsync(Role);
            Assert.IsNull(res);

            var roles = await _adminService.RolesGetAsync();
            Assert.IsNull(roles.Roles.FirstOrDefault(x => x.RoleId == Role.RoleId));
        }

        public async Task RoleAddUserAsync()
        {
            Assert.IsFalse(string.IsNullOrEmpty(User.UserId));
            Assert.IsFalse(string.IsNullOrEmpty(Role.RoleId));

            _userInRole = new()
            {
                RoleId = Role.RoleId,
                UserId = User.UserId
            };

            var res = await _adminService.RoleAddUserAsync(_userInRole);
            Assert.IsNull(res);
            Assert.IsFalse(string.IsNullOrEmpty(_userInRole.UserInRoleId));

            var roles = await _adminService.RolesGetAsync();
            Assert.IsNotNull(roles.UserInRoles.FirstOrDefault(x => x.UserId == User.UserId && x.RoleId == Role.RoleId));
        }

        public async Task DataAccessAddAsync(fx::FreeRP.GrpcService.Core.DataAccess data)
        {
            Assert.IsFalse(string.IsNullOrEmpty(Role.RoleId));

            var res = await _adminService.DataAccessCreateAsync(data);
            Assert.IsNull(res);
        }

        public async Task DataAccessChangeAsync(fx::FreeRP.GrpcService.Core.DataAccess data)
        {
            Assert.IsFalse(string.IsNullOrEmpty(Role.RoleId));

            var res = await _adminService.DataAccessChangeAsync(data);
            Assert.IsNull(res);
        }

        public async Task DataAccessDeleteAsync(fx::FreeRP.GrpcService.Core.DataAccess data)
        {
            Assert.IsFalse(string.IsNullOrEmpty(Role.RoleId));

            var res = await _adminService.DataAccessDeleteAsync(data);
            Assert.IsNull(res);
        }
    }
}
