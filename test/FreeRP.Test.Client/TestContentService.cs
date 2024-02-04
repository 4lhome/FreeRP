using FreeRP.Net.Client.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Test.Client
{
    [TestClass]
    public sealed class TestContentService
    {
        private readonly TestAdminService _adminService = new();
        private readonly Net.Client.Services.ContentService _contentService = new(GlobalSetting.ConnectService, GlobalSetting.I18n);
        private readonly byte[] _file = new byte[8 * 1024 * 1024];

        public GrpcService.Content.ContentUriRequest ContentRoot = new() { Uri = "file:///" };
        public GrpcService.Content.ContentUriRequest ContentUriDirectory { get; set; } = new() { Uri = $"file:///{Path.GetRandomFileName()}" };
        public GrpcService.Content.ContentUriRequest ContentUriFile { get; set; } = new() { Uri = $"file:///{Path.GetRandomFileName()}" };
        public GrpcService.Content.ContentTreeResponse? ContentTreeResponse { get; set; }

        [TestMethod]
        public async Task TestFullAsAdmin()
        {
            Assert.IsTrue(await GlobalSetting.ConnectToServerAsync(), "Connect to server");
            if (await _adminService.LoginAsAdminAsync())
            {
                await CreateDirectoryAsync();
                await ChangeDirectoryAsync();
                
                await UploadFileAsync();
                await DownloadFileAsync();
                await ChangeFileAsync();

                await GetContentTreeResponseAsync();

                Assert.IsNotNull(ContentTreeResponse);
                Assert.IsTrue(ContentTreeResponse.Directories[0].Uri == ContentUriDirectory.Uri);
                Assert.IsTrue(ContentTreeResponse.Files[0].Uri == ContentUriFile.Uri);

                await DeleteDirectoryAsync();
                await DeleteFileAsync();
            }
            else
            {
                Assert.Fail("Login as admin");
            }
        }

        [TestMethod]
        public async Task TestFullAsUser()
        {
            Assert.IsTrue(await GlobalSetting.ConnectToServerAsync(), "Connect to server");
            if (await _adminService.LoginAsAdminAsync())
            {
                await _adminService.UserAddAsync();
                await _adminService.RoleAddAsync();
                await _adminService.RoleAddUserAsync();

                GrpcService.Core.DataAccess dataAccess = new()
                {
                    DataAccessId = "file:///",
                    RoleId = _adminService.Role.RoleId,
                    Change = true,
                    Create = true,
                    Delete = true,
                    Read = true
                };
                await _adminService.DataAccessAddAsync(dataAccess);

                var userService = new Net.Client.Services.UserService(GlobalSetting.ConnectService, GlobalSetting.I18n);
                if (await userService.LoginAsync(_adminService.User))
                {
                    await CreateDirectoryAsync();
                    await ChangeDirectoryAsync();

                    await UploadFileAsync();
                    await DownloadFileAsync();
                    await ChangeFileAsync();

                    await GetContentTreeResponseAsync();

                    Assert.IsNotNull(ContentTreeResponse);
                    Assert.IsTrue(ContentTreeResponse.Directories[0].Uri == ContentUriDirectory.Uri);
                    Assert.IsTrue(ContentTreeResponse.Files[0].Uri == ContentUriFile.Uri);

                    await DeleteDirectoryAsync();
                    await DeleteFileAsync();
                }
                else
                {
                    Assert.Fail("Login as user");
                }
            }
            else
            {
                Assert.Fail("Login as admin");
            }
        }

        public async Task<string?> CreateDirectoryAsync(bool test = true)
        {
            var res = await _contentService.DirectoryCreateAsync(ContentUriDirectory);
            if (test)
                Assert.IsNull(res, nameof(CreateDirectoryAsync));

            return res;
        }

        public async Task<string?> ChangeDirectoryAsync(bool test = true)
        {
            string nPath = $"file:///{Path.GetRandomFileName()}";
            var res = await _contentService.DirectoryChangeAsync(new GrpcService.Content.ChangeContentUriRequest() { DestUri = nPath, SourceUri = ContentUriDirectory.Uri });
            ContentUriDirectory.Uri = nPath;

            if (test)
                Assert.IsNull(res, nameof(ChangeDirectoryAsync));

            return res;
        }

        public async Task<string?> DeleteDirectoryAsync(bool test = true)
        {
            var res = await _contentService.DirectoryDeleteAsync(ContentUriDirectory);

            if (test)
                Assert.IsNull(res, nameof(DeleteDirectoryAsync));

            return res;
        }

        public async Task GetContentTreeResponseAsync(bool test = true)
        {
            var tree = await _contentService.GetContentTreeAsync(ContentRoot);
            if (tree is not null)
                ContentTreeResponse = tree;
            else if (test)
                Assert.Fail(nameof(GetContentTreeResponseAsync));
        }

        public async Task<string?> UploadFileAsync(bool test = true)
        {
            MemoryStream ms = new(_file);
            var res = await _contentService.UploadFileAsync(ms, ContentUriFile);
            await ms.DisposeAsync();

            if (test)
                Assert.IsNull(res, nameof(UploadFileAsync));

            return res;
        }

        public async Task<string?> DownloadFileAsync(bool test = true)
        {
            MemoryStream ms = new();
            var res = await _contentService.DownloadFileAsync(ms, ContentUriFile);

            if (test)
            {
                Assert.IsNull(res, nameof(DownloadFileAsync));
                Assert.IsTrue(ms.Length == _file.Length, nameof(DownloadFileAsync));
            }
            
            return res;
        }

        public async Task<string?> ChangeFileAsync(bool test = true)
        {
            string nPath = $"file:///{Path.GetRandomFileName()}";
            var res = await _contentService.FileChangeAsync(new GrpcService.Content.ChangeContentUriRequest() { DestUri = nPath, SourceUri = ContentUriFile.Uri });
            ContentUriFile.Uri = nPath;

            if (test)
                Assert.IsNull(res, nameof(ChangeFileAsync));

            return res;
        }

        public async Task<string?> DeleteFileAsync(bool test = true)
        {
            var res = await _contentService.FileDeleteAsync(ContentUriFile);

            if (test)
                Assert.IsNull(res, nameof(DeleteFileAsync));

            return res;
        }
    }
}
