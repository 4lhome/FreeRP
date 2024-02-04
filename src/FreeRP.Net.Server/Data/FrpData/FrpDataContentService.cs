using FreeRP.GrpcService.Content;
using FreeRP.GrpcService.Core;
using Google.Protobuf;
using System.Collections.Concurrent;

namespace FreeRP.Net.Server.Data
{
    public partial class FrpDataService : IFrpDataService
    {
        private bool _isRun = false;
        private const int OuterTypeSize = 512;
        public readonly ConcurrentDictionary<string, StreamingData> DownDict = new();
        public readonly ConcurrentDictionary<string, StreamingData> UpDict = new();

        #region Directory

        public Task<Response> DirectoryCreateAsync(ContentUriRequest request, AuthService authService)
        {
            var uri = _uriService.GetUri(request.Uri);
            if (uri is null)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUriSchemeNotSupported });

            var path = _uriService.GetPath(uri);
            if (Directory.Exists(path))
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorExist });

            if (authService.IsAdmin || DataAccessAllowCreate(authService.Roles, uri))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorNone });
                }
                catch (Exception ex)
                {
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUnknown, Message = ex.Message });
                }
            }

            return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorAccessDenied });
        }

        public Task<Response> DirectoryPathChangeAsync(ChangeContentUriRequest request, AuthService authService)
        {
            var sourceUri = _uriService.GetUri(request.SourceUri);
            var destUri = _uriService.GetUri(request.DestUri);

            if (sourceUri is null)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUriSchemeNotSupported, Message = request.SourceUri });

            if (destUri is null)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUriSchemeNotSupported, Message = request.DestUri });

            var sourcePath = _uriService.GetPath(sourceUri);
            if (Directory.Exists(sourcePath) == false)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorNotExist });

            var destPath = _uriService.GetPath(destUri);
            if (Directory.Exists(destPath) && request.Replace == false)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorExist });

            if (authService.IsAdmin || DataAccessAllowChange(authService.Roles, sourceUri))
            {
                try
                {
                    if (request.Copy)
                        Helpers.FileSystem.CopyAll(sourcePath, destPath);
                    else
                        Directory.Move(sourcePath, destPath);
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorNone });
                }
                catch (Exception ex)
                {
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUnknown, Message = ex.Message });
                }
            }

            return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorAccessDenied });
        }

        public Task<Response> DirectoryDeleteAsync(ContentUriRequest request, AuthService authService)
        {
            var uri = _uriService.GetUri(request.Uri);
            if (uri is null)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUriSchemeNotSupported });

            var path = _uriService.GetPath(uri);
            if (Directory.Exists(path) == false)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorNotExist });

            if (authService.IsAdmin || DataAccessAllowDelete(authService.Roles, uri))
            {
                try
                {
                    Directory.Delete(path, true);
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorNone });
                }
                catch (Exception ex)
                {
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUnknown, Message = ex.Message });
                }
            }

            return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorAccessDenied });
        }

        public Task<ContentTreeResponse> GetContentTreeAsync(ContentUriRequest request, AuthService authService)
        {
            try
            {
                var uri = _uriService.GetUri(request.Uri);
                if (uri is null)
                    return Task.FromResult(new ContentTreeResponse() { ErrorType = ErrorType.ErrorUriSchemeNotSupported });

                var path = _uriService.GetPath(uri);
                if (Directory.Exists(path) == false)
                    return Task.FromResult(new ContentTreeResponse() { ErrorType = ErrorType.ErrorNotExist });

                if (authService.IsAdmin || DataAccessAllowRead(authService.Roles, uri))
                {
                    ContentTreeResponse tree = new() { ErrorType = ErrorType.ErrorNone };
                    var di = new DirectoryInfo(path);
                    var dirs = di.GetDirectories();
                    if (dirs is not null && dirs.Length > 0)
                    {
                        foreach (var dir in dirs)
                        {
                            tree.Directories.Add(new FrpDirectory()
                            {
                                Uri = _uriService.Combine(uri.OriginalString, dir.Name),
                                Name = dir.Name,
                                Create = FrpUtcDateTime.FromDateTime(dir.CreationTimeUtc),
                                Change = FrpUtcDateTime.FromDateTime(dir.LastWriteTimeUtc)
                            });
                        }
                    }

                    var files = di.GetFiles();
                    if (files is not null && files.Length > 0)
                    {
                        foreach (var f in files)
                        {
                            tree.Files.Add(new FrpFile()
                            {
                                Uri = _uriService.Combine(uri.OriginalString, f.Name),
                                Name = f.Name,
                                Create = FrpUtcDateTime.FromDateTime(f.CreationTimeUtc),
                                Change = FrpUtcDateTime.FromDateTime(f.LastWriteTimeUtc),
                                Size = (ulong)f.Length
                            });
                        }
                    }

                    return Task.FromResult(tree);
                }
                else
                    return Task.FromResult(new ContentTreeResponse() { ErrorType = ErrorType.ErrorAccessDenied });

            }
            catch (Exception)
            {
                return Task.FromResult(new ContentTreeResponse() { ErrorType = ErrorType.ErrorUnknown });
            }
        }

        #endregion

        #region File

        public Task<ContentStream> FileCreateAsync(ContentUriRequest request, AuthService authService)
        {
            var uri = _uriService.GetUri(request.Uri);
            if (uri is null)
                return Task.FromResult(new ContentStream() { ErrorType = ErrorType.ErrorUriSchemeNotSupported });

            bool allow = false;

            var path = _uriService.GetPath(uri);
            if (Path.Exists(path))
            {
                if (request.Replace == false)
                    return Task.FromResult(new ContentStream() { ErrorType = ErrorType.ErrorFileExist });

                if (authService.IsAdmin || DataAccessAllowChange(authService.Roles, uri))
                {
                    allow = true;
                }
            }
            else if (authService.IsAdmin || DataAccessAllowCreate(authService.Roles, uri))
            {
                allow = true;
            }

            if (allow)
            {
                try
                {
                    var stream = File.Create(path);
                    string id = AddUpload(stream, path);
                    return Task.FromResult(new ContentStream() { ErrorType = ErrorType.ErrorNone, Id = id });
                }
                catch (Exception)
                {
                    return Task.FromResult(new ContentStream() { ErrorType = ErrorType.ErrorUnknown });
                }
            }

            return Task.FromResult(new ContentStream() { ErrorType = ErrorType.ErrorAccessDenied });
        }

        public async Task<ContentStream> FileStreamWriteAsync(ContentStream request)
        {
            try
            {
                if (UpDict.TryGetValue(request.Id, out StreamingData? value))
                {
                    if (value.Stream is not null)
                    {
                        await value.Stream.WriteAsync(request.Data.Memory);
                        
                        if (request.EOF)
                        {
                            UpDict.TryRemove(request.Id, out _);
                            await value.Stream.DisposeAsync();
                        }
                        else
                        {
                            value.Deadline = DateTime.UtcNow.AddMinutes(_frpSettings.GrpcTransportDeadlineMinutes);
                        }

                        return new ContentStream() { ErrorType = ErrorType.ErrorNone, Id = request.Id };
                    }
                    else
                    {
                        UpDict.TryRemove(request.Id, out _);
                    }
                }

                return new ContentStream() { ErrorType = ErrorType.ErrorNotExist };
            }
            catch (Exception)
            {
                return new ContentStream() { ErrorType = ErrorType.ErrorUnknown };
            }
        }

        public Task<ContentStream> FileOpenAsync(ContentUriRequest request, AuthService authService)
        {
            var uri = _uriService.GetUri(request.Uri);
            if (uri is null)
                return Task.FromResult(new ContentStream() { ErrorType = ErrorType.ErrorUriSchemeNotSupported });

            var path = _uriService.GetPath(uri);
            if (File.Exists(path))
            {
                if (authService.IsAdmin || DataAccessAllowRead(authService.Roles, uri))
                {
                    try
                    {
                        var stream = File.OpenRead(path);
                        string id = AddDownload(stream, path);
                        return Task.FromResult(new ContentStream() { ErrorType = ErrorType.ErrorNone, Id = id });
                    }
                    catch (Exception)
                    {
                        return Task.FromResult(new ContentStream() { ErrorType = ErrorType.ErrorUnknown });
                    }
                }
                else
                {
                    return Task.FromResult(new ContentStream() { ErrorType = ErrorType.ErrorAccessDenied });
                }
            }
            else
            {
                return Task.FromResult(new ContentStream() { ErrorType = ErrorType.ErrorFileNotExist });
            }
        }

        public async Task<ContentStream> FileStreamReadAsync(ContentStream contentStream)
        {
            try
            {
                if (DownDict.TryGetValue(contentStream.Id, out StreamingData? value))
                {
                    if (value.Stream is not null)
                    {
                        ContentStream msg = new() { Id = contentStream.Id };
                        var buffer = new byte[_frpSettings.GrpcMessageSize - OuterTypeSize];
                        var count = await value.Stream.ReadAsync(buffer);
                        if (count < buffer.Length)
                        {
                            msg.EOF = true;
                            DownDict.TryRemove(contentStream.Id, out _);
                            await value.Stream.DisposeAsync();
                        }
                        else
                        {
                            value.Deadline = DateTime.UtcNow.AddMinutes(_frpSettings.GrpcTransportDeadlineMinutes);
                        }

                        if (count > 0)
                        {
                            msg.Data = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, count));
                        }

                        msg.ErrorType = ErrorType.ErrorNone;
                        return msg;
                    }
                    else
                    {
                        DownDict.TryRemove(contentStream.Id, out _);
                    }
                }

                return new ContentStream() { ErrorType = ErrorType.ErrorNotExist, EOF = true };
            }
            catch (Exception)
            {
                return new ContentStream() { ErrorType = ErrorType.ErrorUnknown, EOF = true };
            }
        }

        public Task<Response> FilePathChangeAsync(ChangeContentUriRequest request, AuthService authService)
        {
            var sourceUri = _uriService.GetUri(request.SourceUri);
            var destUri = _uriService.GetUri(request.DestUri);

            if (sourceUri is null)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUriSchemeNotSupported, Message = request.SourceUri });

            if (destUri is null)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUriSchemeNotSupported, Message = request.DestUri });

            var sourcePath = _uriService.GetPath(sourceUri);
            if (File.Exists(sourcePath) == false)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorNotExist });

            var destPath = _uriService.GetPath(destUri);
            if (File.Exists(destPath) && request.Replace == false)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorExist });

            if (authService.IsAdmin || DataAccessAllowChange(authService.Roles, sourceUri))
            {
                try
                {
                    if (request.Copy)
                        File.Copy(sourcePath, destPath, true);
                    else
                        File.Move(sourcePath, destPath, true);
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorNone });
                }
                catch (Exception ex)
                {
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUnknown, Message = ex.Message });
                }
            }

            return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorAccessDenied });
        }

        public Task<Response> FileDeleteAsync(ContentUriRequest request, AuthService authService)
        {
            var uri = _uriService.GetUri(request.Uri);
            if (uri is null)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUriSchemeNotSupported });

            var path = _uriService.GetPath(uri);
            if (File.Exists(path) == false)
                return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorNotExist });

            if (authService.IsAdmin || DataAccessAllowDelete(authService.Roles, uri))
            {
                try
                {
                    File.Delete(path);
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorNone });
                }
                catch (Exception ex)
                {
                    return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorUnknown, Message = ex.Message });
                }
            }

            return Task.FromResult(new Response() { ErrorType = ErrorType.ErrorAccessDenied });
        }

        #endregion

        #region Worker

        public string AddDownload(Stream stream, string file)
        {
            string id = Guid.NewGuid().ToString();
            DownDict[id] = new StreamingData()
            {
                Stream = stream,
                File = file,
                Deadline = DateTime.UtcNow.AddMinutes(_frpSettings.GrpcTransportDeadlineMinutes)
            };
            Task.Run(DeadLineWorker);

            return id;
        }

        public string AddUpload(Stream stream, string file)
        {
            string id = Guid.NewGuid().ToString();
            UpDict[id] = new StreamingData()
            {
                Stream = stream,
                File = file,
                Deadline = DateTime.UtcNow.AddMinutes(_frpSettings.GrpcTransportDeadlineMinutes)
            };
            Task.Run(DeadLineWorker);
            return id;
        }

        private async void DeadLineWorker()
        {
            if (_isRun)
                return;

            _isRun = true;

            while (_isRun)
            {
                await Task.Delay(1000);

                foreach (var item in DownDict)
                {
                    if (DateTime.UtcNow > item.Value.Deadline)
                    {
                        if (item.Value.Stream is not null)
                        {
                            await item.Value.Stream.DisposeAsync();
                        }

                        DownDict.TryRemove(item);
                    }
                }

                foreach (var item in UpDict)
                {
                    if (DateTime.UtcNow > item.Value.Deadline)
                    {
                        if (item.Value.Stream is not null)
                        {
                            await item.Value.Stream.DisposeAsync();
                        }

                        try
                        {
                            if (File.Exists(item.Value.File))
                                File.Delete(item.Value.File);

                            UpDict.TryRemove(item);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                if (DownDict.IsEmpty && UpDict.IsEmpty)
                    _isRun = false;
            }
        }

        #endregion
    }
}
