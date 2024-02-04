using FreeRP.GrpcService.Core;
using FreeRP.GrpcService.Content;
using FreeRP.Net.Server.Data;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace FreeRP.Net.Server.GrpcServices
{
    public class ContentService(IFrpDataService appData, AuthService authService) : GrpcService.Content.ContentService.ContentServiceBase
    {
        private readonly IFrpDataService _appData = appData;
        private readonly AuthService _authService = authService;

        [Authorize]
        public override async Task<Response> DirectoryCreate(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.DirectoryCreateAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> DirectoryPathChange(ChangeContentUriRequest request, ServerCallContext context)
        {
            return await _appData.DirectoryPathChangeAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> DirectoryDelete(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.DirectoryDeleteAsync(request, _authService);
        }

        [Authorize]
        public override async Task<ContentTreeResponse> GetContentTree(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.GetContentTreeAsync(request, _authService);
        }

        [Authorize]
        public override async Task<ContentStream> FileCreate(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FileCreateAsync(request, _authService);
        }

        [Authorize]
        public override async Task<ContentStream> FileStreamWrite(ContentStream request, ServerCallContext context)
        {
            return await _appData.FileStreamWriteAsync(request);
        }

        [Authorize]
        public override async Task<ContentStream> FileOpen(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FileOpenAsync(request, _authService);
        }

        [Authorize]
        public override async Task<ContentStream> FileStreamRead(ContentStream request, ServerCallContext context)
        {
            return await _appData.FileStreamReadAsync(request);
        }

        [Authorize]
        public override async Task<Response> FilePathChange(ChangeContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FilePathChangeAsync(request, _authService);
        }

        [Authorize]
        public override async Task<Response> FileDelete(ContentUriRequest request, ServerCallContext context)
        {
            return await _appData.FileDeleteAsync(request, _authService);
        }
    }
}
