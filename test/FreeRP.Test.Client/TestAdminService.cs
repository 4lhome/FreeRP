using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Test.Client
{
    [TestClass]
    public sealed class TestAdminService
    {
        public GrpcService.Core.User User { get; set; } = new();
        public GrpcService.Core.Role Role { get; set; } = new();

        private readonly GrpcService.Core.User _adminUser = new() { Email = "admin", Password = "admin" };
        private GrpcService.Core.UserInRole _userInRole = new();
        private readonly Net.Client.Services.AdminService _adminService = new(GlobalSetting.ConnectService, GlobalSetting.I18n);

        [TestMethod]
        public async Task TestFull()
        {
            Assert.IsTrue(await GlobalSetting.ConnectToServerAsync(), "Connect to server");
            Assert.IsTrue(await LoginAsAdminAsync(), "Login as admin");

            await UserAddAsync();
            await UserChangeAsync();
            
            await RoleAddAsync();
            await RoleChangeAsync();

            await RoleAddUserAsync();

            GrpcService.Core.DataAccess dataAccess = new()
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
            Assert.IsNull(res, nameof(UserAddAsync));
            Assert.IsFalse(string.IsNullOrEmpty(User.UserId), nameof(UserAddAsync));

            var users = await _adminService.UsersGetAsync();
            Assert.IsNotNull(users.Users.FirstOrDefault(x => x.Email == User.Email), nameof(UserAddAsync));
            User.Password = password;
        }

        public async Task UserChangeAsync()
        {
            User.FirstName = Path.GetRandomFileName();
            await _adminService.UserChangeAsync(User);

            var users = await _adminService.UsersGetAsync();
            Assert.IsNotNull(users.Users.FirstOrDefault(x => x.Email == User.Email), nameof(UserChangeAsync));

            User.Password = "ChangeTestPass123!";
            var res = await _adminService.UserChangePasswordAsync(User);
            Assert.IsNull(res, nameof(UserChangeAsync));
        }

        public async Task UserDeleteAsync()
        {
            var res = await _adminService.UserDeleteAsync(User);
            Assert.IsNull(res, nameof(UserDeleteAsync));

            var users = await _adminService.UsersGetAsync();
            Assert.IsNull(users.Users.FirstOrDefault(x => x.Email == User.Email), nameof(UserDeleteAsync));
        }

        public async Task RoleAddAsync()
        {
            Role = new()
            {
                Name = Path.GetRandomFileName()
            };

            var res = await _adminService.RoleAddAsync(Role);
            Assert.IsNull(res, nameof(RoleAddAsync));
            Assert.IsFalse(string.IsNullOrEmpty(Role.RoleId), nameof(RoleAddAsync));
        }

        public async Task RoleChangeAsync()
        {
            Role.Name = Path.GetRandomFileName();
            var res = await _adminService.RoleChangeAsync(Role);
            Assert.IsNull(res, nameof(RoleChangeAsync));

            var roles = await _adminService.RolesGetAsync();
            Assert.IsNotNull(roles.Roles.FirstOrDefault(x => x.Name == Role.Name), nameof(RoleChangeAsync));
        }

        public async Task RoleDeleteAsync()
        {
            var res = await _adminService.RoleDeleteAsync(Role);
            Assert.IsNull(res, nameof(RoleDeleteAsync));

            var roles = await _adminService.RolesGetAsync();
            Assert.IsNull(roles.Roles.FirstOrDefault(x => x.RoleId == Role.RoleId), nameof(RoleDeleteAsync));
        }

        public async Task RoleAddUserAsync()
        {
            Assert.IsFalse(string.IsNullOrEmpty(User.UserId), nameof(RoleAddUserAsync));
            Assert.IsFalse(string.IsNullOrEmpty(Role.RoleId), nameof(RoleAddUserAsync));

            _userInRole = new() { 
                RoleId = Role.RoleId,
                UserId = User.UserId
            };

            var res = await _adminService.RoleAddUserAsync(_userInRole);
            Assert.IsNull(res, nameof(RoleAddUserAsync));
            Assert.IsFalse(string.IsNullOrEmpty(_userInRole.UserInRoleId), nameof(RoleAddUserAsync));

            var roles = await _adminService.RolesGetAsync();
            Assert.IsNotNull(roles.UserInRoles.FirstOrDefault(x => x.UserId == User.UserId && x.RoleId == Role.RoleId), nameof(RoleAddUserAsync));
        }

        public async Task DataAccessAddAsync(GrpcService.Core.DataAccess data)
        {
            Assert.IsFalse(string.IsNullOrEmpty(Role.RoleId));

            var res = await _adminService.DataAccessCreateAsync(data);
            Assert.IsNull(res, nameof(DataAccessAddAsync));
        }

        public async Task DataAccessChangeAsync(GrpcService.Core.DataAccess data)
        {
            Assert.IsFalse(string.IsNullOrEmpty(Role.RoleId), nameof(DataAccessChangeAsync));

            var res = await _adminService.DataAccessChangeAsync(data);
            Assert.IsNull(res, nameof(DataAccessChangeAsync));
        }

        public async Task DataAccessDeleteAsync(GrpcService.Core.DataAccess data)
        {
            Assert.IsFalse(string.IsNullOrEmpty(Role.RoleId), nameof(DataAccessDeleteAsync));

            var res = await _adminService.DataAccessDeleteAsync(data);
            Assert.IsNull(res, nameof(DataAccessDeleteAsync));
        }
    }
}
