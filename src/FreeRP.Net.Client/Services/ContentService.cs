using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Services
{
    public class ContentService(ConnectService connectService, Translation.I18nService i18n)
    {
        private readonly ConnectService _connectService = connectService;
        private readonly Translation.I18nService _i18n = i18n;

        #region Directory

        public async ValueTask<string?> DirectoryCreateAsync(GrpcService.Content.ContentUriRequest dir, CancellationToken ct = default)
        {
            try
            {
                var res = await _connectService.ContentServiceClient.DirectoryCreateAsync(dir, _connectService.AuthHeader, cancellationToken: ct);
                return res.ErrorType switch
                {
                    GrpcService.Core.ErrorType.ErrorUriSchemeNotSupported => _i18n.Text.UriSchemeNotSupported,
                    GrpcService.Core.ErrorType.ErrorExist => _i18n.Text.XExist.Replace("{0}", dir.Uri),
                    GrpcService.Core.ErrorType.ErrorAccessDenied => _i18n.Text.AccessDenied,
                    GrpcService.Core.ErrorType.ErrorUnknown => res.Message,
                    _ => null,
                };
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async ValueTask<string?> DirectoryChangeAsync(GrpcService.Content.ChangeContentUriRequest request, CancellationToken ct = default)
        {
            try
            {
                var res = await _connectService.ContentServiceClient.DirectoryPathChangeAsync(request, _connectService.AuthHeader, cancellationToken: ct);
                return res.ErrorType switch
                {
                    GrpcService.Core.ErrorType.ErrorUriSchemeNotSupported => _i18n.Text.UriSchemeNotSupported,
                    GrpcService.Core.ErrorType.ErrorNotExist => _i18n.Text.XNotExist.Replace("{0}", request.SourceUri),
                    GrpcService.Core.ErrorType.ErrorExist => _i18n.Text.XExist.Replace("{0}", request.DestUri),
                    GrpcService.Core.ErrorType.ErrorAccessDenied => _i18n.Text.AccessDenied,
                    GrpcService.Core.ErrorType.ErrorUnknown => res.Message,
                    _ => null,
                };
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async ValueTask<string?> DirectoryDeleteAsync(GrpcService.Content.ContentUriRequest dir, CancellationToken ct = default)
        {
            try
            {
                var res = await _connectService.ContentServiceClient.DirectoryDeleteAsync(dir, _connectService.AuthHeader, cancellationToken: ct);
                return res.ErrorType switch
                {
                    GrpcService.Core.ErrorType.ErrorUriSchemeNotSupported => _i18n.Text.UriSchemeNotSupported,
                    GrpcService.Core.ErrorType.ErrorNotExist => _i18n.Text.XNotExist.Replace("{0}", dir.Uri),
                    GrpcService.Core.ErrorType.ErrorAccessDenied => _i18n.Text.AccessDenied,
                    GrpcService.Core.ErrorType.ErrorUnknown => res.Message,
                    _ => null,
                };
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async ValueTask<GrpcService.Content.ContentTreeResponse?> GetContentTreeAsync(GrpcService.Content.ContentUriRequest uri, CancellationToken ct = default)
        {
            return await _connectService.ContentServiceClient.GetContentTreeAsync(uri, _connectService.AuthHeader, cancellationToken: ct);
        }

        #endregion

        #region File

        public async ValueTask<string?> UploadFileAsync(Stream stream, GrpcService.Content.ContentUriRequest uri, bool replaceIfExist = false, CancellationToken ct = default)
        {
            try
            {
                if (_connectService.ContentServiceClient is not null)
                {
                    var res = await _connectService.ContentServiceClient.FileCreateAsync(
                        new GrpcService.Content.ContentUriRequest() { Uri = uri.Uri, Replace = replaceIfExist }, _connectService.AuthHeader, cancellationToken: ct);

                    if (res.ErrorType == GrpcService.Core.ErrorType.ErrorNone)
                    {
                        var buffer = new byte[_connectService.ConnectResponse.GrpcMessageSize - 512];

                        while (true)
                        {
                            int count = await stream.ReadAsync(buffer, ct);
                            GrpcService.Content.ContentStream trans = new()
                            {
                                Data = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, count)),
                                Id = res.Id
                            };

                            if (count < buffer.Length)
                                trans.EOF = true;

                            var msg = await _connectService.ContentServiceClient.FileStreamWriteAsync(trans, _connectService.AuthHeader, cancellationToken: ct);
                            if (msg.ErrorType is not GrpcService.Core.ErrorType.ErrorNone)
                                break;

                            if (trans.EOF)
                                break;
                        }
                        return null;
                    }
                    else if (res.ErrorType == GrpcService.Core.ErrorType.ErrorUriSchemeNotSupported)
                        return _i18n.Text.UriSchemeNotSupported;
                    else if (res.ErrorType == GrpcService.Core.ErrorType.ErrorFileExist)
                        return _i18n.Text.XExist.Replace("{0}", uri.Uri);
                    else if (res.ErrorType == GrpcService.Core.ErrorType.ErrorAccessDenied)
                        return _i18n.Text.AccessDenied;
                    else
                        return null;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return _i18n.Text.UnknownError;
        }

        public async ValueTask<string?> DownloadFileAsync(Stream stream, GrpcService.Content.ContentUriRequest uri, CancellationToken ct = default)
        {
            try
            {
                if (_connectService.ContentServiceClient is not null)
                {
                    var res = await _connectService.ContentServiceClient.FileOpenAsync(uri, _connectService.AuthHeader, cancellationToken: ct);
                    if (res.ErrorType == GrpcService.Core.ErrorType.ErrorNone)
                    {
                        while (true)
                        {
                            var trans = await _connectService.ContentServiceClient
                                .FileStreamReadAsync(new GrpcService.Content.ContentStream() { Id = res.Id }, _connectService.AuthHeader, cancellationToken: ct);

                            if (trans.ErrorType is GrpcService.Core.ErrorType.ErrorNone)
                            {
                                await stream.WriteAsync(trans.Data.Memory, ct);
                            }

                            if (trans.EOF)
                                return null;
                        }
                    }
                    else if (res.ErrorType == GrpcService.Core.ErrorType.ErrorUriSchemeNotSupported)
                        return _i18n.Text.UriSchemeNotSupported;
                    else if (res.ErrorType == GrpcService.Core.ErrorType.ErrorFileNotExist)
                        return _i18n.Text.XNotExist.Replace("{0}", uri.Uri);
                    else if (res.ErrorType == GrpcService.Core.ErrorType.ErrorAccessDenied)
                        return _i18n.Text.AccessDenied;
                    else
                        return null;

                }
            }
            catch (Exception)
            {
            }

            return _i18n.Text.UnknownError;
        }

        public async ValueTask<string?> FileChangeAsync(GrpcService.Content.ChangeContentUriRequest request, CancellationToken ct = default)
        {
            try
            {
                var res = await _connectService.ContentServiceClient.FilePathChangeAsync(request, _connectService.AuthHeader, cancellationToken: ct);
                return res.ErrorType switch
                {
                    GrpcService.Core.ErrorType.ErrorUriSchemeNotSupported => _i18n.Text.UriSchemeNotSupported,
                    GrpcService.Core.ErrorType.ErrorNotExist => _i18n.Text.XNotExist.Replace("{0}", request.SourceUri),
                    GrpcService.Core.ErrorType.ErrorExist => _i18n.Text.XExist.Replace("{0}", request.DestUri),
                    GrpcService.Core.ErrorType.ErrorAccessDenied => _i18n.Text.AccessDenied,
                    GrpcService.Core.ErrorType.ErrorUnknown => res.Message,
                    _ => null,
                };
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async ValueTask<string?> FileDeleteAsync(GrpcService.Content.ContentUriRequest dir, CancellationToken ct = default)
        {
            try
            {
                var res = await _connectService.ContentServiceClient.FileDeleteAsync(dir, _connectService.AuthHeader, cancellationToken: ct);
                return res.ErrorType switch
                {
                    GrpcService.Core.ErrorType.ErrorUriSchemeNotSupported => _i18n.Text.UriSchemeNotSupported,
                    GrpcService.Core.ErrorType.ErrorDirectoryNotExist => _i18n.Text.XNotExist.Replace("{0}", dir.Uri),
                    GrpcService.Core.ErrorType.ErrorAccessDenied => _i18n.Text.AccessDenied,
                    GrpcService.Core.ErrorType.ErrorUnknown => res.Message,
                    _ => null,
                };
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        #endregion
    }
}
