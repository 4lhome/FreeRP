namespace FreeRP.Net.Server.Data
{
    public interface IFrpDataService : IDisposable
    {
        public GrpcService.Database.Database Database { get; set; }

        #region User

        GrpcService.Core.User UserGetByEmail(string name);
        IEnumerable<GrpcService.Core.User> UserGetAll();
        IEnumerable<GrpcService.Core.User> UserApiGetAll();
        bool UserCheckPassword(GrpcService.Core.User user);
        IEnumerable<GrpcService.Core.Role> UserGetRoles(GrpcService.Core.User user);
        ValueTask<GrpcService.Core.ErrorType> UserAddAsync(GrpcService.Core.User u);
        ValueTask<GrpcService.Core.ErrorType> UserChangeAsync(GrpcService.Core.User user, bool admin);
        ValueTask<GrpcService.Core.ErrorType> UserChangePasswordAsync(GrpcService.Core.User user, bool admin);
        ValueTask<GrpcService.Core.ErrorType> UserDeleteAsync(GrpcService.Core.User u);

        #endregion

        #region Role

        IEnumerable<GrpcService.Core.Role> RolesGetAll();
        IEnumerable<GrpcService.Core.UserInRole> UserInRoleGetAll();
        GrpcService.Core.Role? RoleGet(string roleId);
        ValueTask<GrpcService.Core.ErrorType> RoleAddAsync(GrpcService.Core.Role role);
        ValueTask<GrpcService.Core.ErrorType> RoleAddUserAsync(GrpcService.Core.UserInRole userInRole);
        ValueTask<GrpcService.Core.ErrorType> RoleChangeAsync(GrpcService.Core.Role role);
        ValueTask<GrpcService.Core.ErrorType> RoleDeleteAsync(GrpcService.Core.Role role);
        ValueTask<GrpcService.Core.ErrorType> RoleDeleteUserAsync(GrpcService.Core.UserInRole userInRole);

        #endregion

        #region Data access

        ValueTask<GrpcService.Core.ErrorType> DataAccessAddAsync(GrpcService.Core.DataAccess data);
        ValueTask<GrpcService.Core.ErrorType> DataAccessChangeAsync(GrpcService.Core.DataAccess data);
        ValueTask<GrpcService.Core.ErrorType> DataAccessDeleteAsync(GrpcService.Core.DataAccess data);
        bool DataAccessAllowRead(IEnumerable<GrpcService.Core.Role> roles, Uri uri);
        bool DataAccessAllowCreate(IEnumerable<GrpcService.Core.Role> roles, Uri uri);
        bool DataAccessAllowDelete(IEnumerable<GrpcService.Core.Role> roles, Uri uri);
        bool DataAccessAllowChange(IEnumerable<GrpcService.Core.Role> roles, Uri uri);
        GrpcService.Core.DataAccess GetDataAccess(GrpcService.Core.Role role, Uri uri);

        #endregion

        #region Content

        Task<GrpcService.Core.Response> DirectoryCreateAsync(GrpcService.Content.ContentUriRequest request, AuthService authService);
        Task<GrpcService.Core.Response> DirectoryPathChangeAsync(GrpcService.Content.ChangeContentUriRequest request, AuthService authService);
        Task<GrpcService.Core.Response> DirectoryDeleteAsync(GrpcService.Content.ContentUriRequest request, AuthService authService);
        Task<GrpcService.Content.ContentTreeResponse> GetContentTreeAsync(GrpcService.Content.ContentUriRequest request, AuthService authService);

        Task<GrpcService.Content.ContentStream> FileCreateAsync(GrpcService.Content.ContentUriRequest request, AuthService authService);
        Task<GrpcService.Content.ContentStream> FileStreamWriteAsync(GrpcService.Content.ContentStream request);
        Task<GrpcService.Content.ContentStream> FileOpenAsync(GrpcService.Content.ContentUriRequest request, AuthService authService);
        Task<GrpcService.Content.ContentStream> FileStreamReadAsync(GrpcService.Content.ContentStream contentStream);
        Task<GrpcService.Core.Response> FilePathChangeAsync(GrpcService.Content.ChangeContentUriRequest request, AuthService authService);
        Task<GrpcService.Core.Response> FileDeleteAsync(GrpcService.Content.ContentUriRequest request, AuthService authService);

        #endregion

        #region Database

        IEnumerable<GrpcService.Database.Database> DatabaseGetAll();
        GrpcService.Database.Database? DatabaseGet(string databaseId);
        GrpcService.Database.Database? DatabaseGetWithAccess(string databaseId, IEnumerable<GrpcService.Core.Role> roles, bool isAdmin);
        ValueTask<GrpcService.Core.ErrorType> DatabaseAdd(GrpcService.Database.Database database, string id);
        ValueTask<GrpcService.Core.ErrorType> DatabaseChangeAsync(GrpcService.Database.Database ndb, string id);
        ValueTask<GrpcService.Core.ErrorType> DatabaseDeleteAsync(GrpcService.Database.Database ndb, string id);

        ValueTask<GrpcService.Core.ErrorType> DatabaseOpenAsync(GrpcService.Database.Database database, string id);
        ValueTask<GrpcService.Core.ErrorType> DatabaseSaveChangesAsync(GrpcService.Database.Database database, string id);
        ValueTask<GrpcService.Core.ErrorType> DatabaseItemAddAsync(GrpcService.Database.DataRequest dataRequest, GrpcService.Database.Database database);
        ValueTask<GrpcService.Core.ErrorType> DatabaseItemUpdateAsync(GrpcService.Database.DataRequest dataRequest, GrpcService.Database.Database database);
        ValueTask<GrpcService.Core.ErrorType> DatabaseItemRemoveAsync(GrpcService.Database.DataRequest dataRequest, GrpcService.Database.Database database);
        ValueTask<GrpcService.Core.ErrorType> DatabaseItemQueryAsync(GrpcService.Database.QueryRequest queryRequest, GrpcService.Database.Database database);

        #endregion

        #region Plugin

        public IEnumerable<GrpcService.Plugin.Plugin> PluginsGetAll();
        IEnumerable<GrpcService.Plugin.PluginRole> PluginRolesGetAll();
        GrpcService.Core.Response PluginInstall(string filePath);
        GrpcService.Core.ErrorType PluginRoleAdd(GrpcService.Plugin.PluginRole role);
        GrpcService.Core.ErrorType PluginRoleDelete(GrpcService.Plugin.PluginRole role);

        #endregion
    }
}
