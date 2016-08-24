/*
The MIT License(MIT)

Copyright(c) 2015 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaFireSDK;
using MediaFireSDK.Model;
using MediaFireSDK.Model.Errors;
using MediaFireSDK.Model.Responses;
using IgorSoft.CloudFS.Gateways.MediaFire.Auth;
using IgorSoft.CloudFS.Interface;
using IgorSoft.CloudFS.Interface.Composition;
using IgorSoft.CloudFS.Interface.IO;

namespace IgorSoft.CloudFS.Gateways.MediaFire
{
    [ExportAsAsyncCloudGateway("MediaFire")]
    [ExportMetadata(nameof(CloudGatewayMetadata.CloudService), MediaFireGateway.SCHEMA)]
    [ExportMetadata(nameof(CloudGatewayMetadata.Capabilities), MediaFireGateway.CAPABILITIES)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ServiceUri), MediaFireGateway.URL)]
    [ExportMetadata(nameof(CloudGatewayMetadata.ApiAssembly), nameof(MediaFireSDK))]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public sealed class MediaFireGateway : IAsyncCloudGateway, IPersistGatewaySettings, IDisposable
    {
        private const string SCHEMA = "mediafire";

        private const GatewayCapabilities CAPABILITIES = GatewayCapabilities.All ^ GatewayCapabilities.ClearContent ^ GatewayCapabilities.CopyDirectoryItem;

        private const string URL = "https://www.mediafire.com";

        private const int RETRIES = 3;

        private class MediaFireContext
        {
            public MediaFireAgent Agent { get; }

            public MediaFireContext(MediaFireAgent agent)
            {
                Agent = agent;
            }
        }

        private readonly IDictionary<RootName, MediaFireContext> contextCache = new Dictionary<RootName, MediaFireContext>();

        private string settingsPassPhrase;

        [ImportingConstructor]
        public MediaFireGateway([Import(ExportContracts.SettingsPassPhrase)] string settingsPassPhrase)
        {
            this.settingsPassPhrase = settingsPassPhrase;
        }

        private async Task<MediaFireContext> RequireContextAsync(RootName root, string apiKey = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var result = default(MediaFireContext);
            if (!contextCache.TryGetValue(root, out result)) {
                var agent = await Authenticator.LoginAsync(root.UserName, apiKey, settingsPassPhrase);
                contextCache.Add(root, result = new MediaFireContext(agent));
            }
            return result;
        }

        public async Task<bool> TryAuthenticateAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            try {
                await RequireContextAsync(root, apiKey);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public async Task<DriveInfoContract> GetDriveAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root, apiKey);

            var item = await AsyncFunc.RetryAsync<MediaFireGetUserInfoResponse, MediaFireApiException>(async ()
                => await context.Agent.GetAsync<MediaFireGetUserInfoResponse>(MediaFireApiUserMethods.GetInfo), RETRIES);

            return new DriveInfoContract(item.UserDetails.Email, item.UserDetails.StorageLimit - item.UserDetails.UsedStorageSize, item.UserDetails.UsedStorageSize);
        }

        public async Task<RootDirectoryInfoContract> GetRootAsync(RootName root, string apiKey, IDictionary<string, string> parameters)
        {
            var context = await RequireContextAsync(root);

            var item = await context.Agent.GetAsync<MediaFireGetFolderInfoResponse>(MediaFireApiFolderMethods.GetInfo);

            return new RootDirectoryInfoContract(item.FolderInfo.FolderKey, DateTimeOffset.FromFileTime(0), DateTimeOffset.FromFileTime(0));
        }

        public async Task<IEnumerable<FileSystemInfoContract>> GetChildItemAsync(RootName root, DirectoryId parent)
        {
            var context = await RequireContextAsync(root);

            var foldersItem = await context.Agent.GetAsync<MediaFireGetContentResponse>(MediaFireApiFolderMethods.GetContent, new Dictionary<string, object>() {
                { MediaFireApiParameters.FolderKey, parent.Value },
                { MediaFireApiParameters.ContentType, MediaFireFolderContentType.Folders.ToApiParameter() }
            });
            var filesItem = await context.Agent.GetAsync<MediaFireGetContentResponse>(MediaFireApiFolderMethods.GetContent, new Dictionary<string, object>() {
                { MediaFireApiParameters.FolderKey, parent.Value },
                { MediaFireApiParameters.ContentType, MediaFireFolderContentType.Files.ToApiParameter() }
            });

            return (foldersItem.FolderContent.Folders?.Select(f => f.ToDirectoryInfoContract()).Cast<FileSystemInfoContract>() ?? Enumerable.Empty< FileSystemInfoContract>())
                .Concat(filesItem.FolderContent.Files?.Select(f => f.ToFileInfoContract()) ?? Enumerable.Empty<FileSystemInfoContract>());
        }

        public Task<bool> ClearContentAsync(RootName root, FileId target, Func<FileSystemInfoLocator> locatorResolver)
        {
            return Task.FromException<bool>(new NotSupportedException(Properties.Resources.EmptyFilesNotSupported));
        }

        public async Task<Stream> GetContentAsync(RootName root, FileId source)
        {
            var context = await RequireContextAsync(root);

            var links = await context.Agent.GetAsync<MediaFireGetLinksResponse>(MediaFireApiFileMethods.GetLinks, new Dictionary<string, object>() {
                { MediaFireApiParameters.QuickKey, source.Value },
                { MediaFireApiParameters.LinkType, MediaFireSDK.MediaFireApiConstants.LinkTypeDirectDownload }
            });

            if (!links.Links.Any() && string.IsNullOrEmpty(links.Links[0].DirectDownload))
                throw new MediaFireException(MediaFireErrorMessages.FileMustContainADirectLink);

            var response = await new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Get, links.Links[0].DirectDownload), HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();

            return stream;
        }

        public async Task<bool> SetContentAsync(RootName root, FileId target, Stream content, IProgress<ProgressValue> progress, Func<FileSystemInfoLocator> locatorResolver)
        {
            if (locatorResolver == null)
                throw new ArgumentNullException(nameof(locatorResolver));

            var context = await RequireContextAsync(root);

            var locator = locatorResolver();
            var config = await context.Agent.Upload.GetUploadConfiguration(locator.Name, content.Length, locator.ParentId.Value, MediaFireActionOnDuplicate.Skip);
            config.Endpoint = config.Endpoint
                .Remove(config.Endpoint.IndexOf($"&{MediaFireApiParameters.FolderKey}".ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCulture))
                .Replace($"{MediaFireApiUploadMethods.Simple}?".ToString(CultureInfo.InvariantCulture), $"{MediaFireApiUploadMethods.Update}?{MediaFireApiParameters.QuickKey}={target.Value}&".ToString(CultureInfo.InvariantCulture));
            var mediaFireProgress = progress != null ? new Progress<MediaFireOperationProgress>(p => progress.Report(new ProgressValue((int)p.CurrentSize, (int)p.TotalSize))) : null;
            var upload = await context.Agent.Upload.Simple(config, content, mediaFireProgress);
            //TODO: Fix code for direct execution of upload/update via PostStreamAsync<>()
            //var upload = (await context.Agent.PostStreamAsync<UploadResponse>(MediaFireApiUploadMethods.Update, content, new Dictionary<string, object>() {
            //    { MediaFireApiParameters.QuickKey, target.Value }
            //},  new Dictionary<string, string>() {
            //    { MediaFireApiConstants.ContentTypeHeader, MediaFireApiConstants.SimpleUploadContentTypeValue }
            //})).DoUpload;

            if (!upload.IsSuccess)
                throw new MediaFireException(string.Format(System.Globalization.CultureInfo.CurrentCulture, MediaFireErrorMessages.UploadErrorFormat, upload.Result));
            while (!(upload.IsComplete && upload.IsSuccess)) {
                await Task.Delay(100);
                upload = await context.Agent.Upload.PollUpload(upload.Key);
            }
            var item = await context.Agent.GetAsync<MediaFireGetFileInfoResponse>(MediaFireApiFileMethods.GetInfo, new Dictionary<string, object>() {
                { MediaFireApiParameters.QuickKey, upload.QuickKey }
            });

            return true;
        }

        public async Task<FileSystemInfoContract> CopyItemAsync(RootName root, FileSystemId source, string copyName, DirectoryId destination, bool recurse)
        {
            var context = await RequireContextAsync(root);

            if (source is DirectoryId) {
                //TODO: Fix code for copying of directories
                //var copy = await context.Agent.GetAsync<ApiExtensions.MediaFireCopyFolderResponse>(MediaFireApiFolderMethods.Copy, new Dictionary<string, object>() {
                //    { MediaFireApiParameters.FolderKeySource, source.Value },
                //    { MediaFireApiParameters.FolderKeyDestination, destination.Value }
                //});

                //var newFolderKey = copy.NewFolderKeys[0];

                //if (!string.IsNullOrEmpty(copyName)) {
                //    await context.Agent.GetAsync<MediaFireEmptyResponse>(MediaFireApiFolderMethods.Update, new Dictionary<string, object>() {
                //        { MediaFireApiParameters.FolderKey, newFolderKey },
                //        { MediaFireApiParameters.FolderName, copyName }
                //    });
                //}
                //var item = await context.Agent.GetAsync<MediaFireGetFolderInfoResponse>(MediaFireApiFolderMethods.GetInfo, new Dictionary<string, object>() {
                //    { MediaFireApiParameters.FolderKey, newFolderKey }
                //});

                //return new DirectoryInfoContract(item.FolderInfo.FolderKey, item.FolderInfo.Name, item.FolderInfo.Created, item.FolderInfo.Created);

                throw new NotSupportedException(Properties.Resources.CopyingOfDirectoriesNotSupported);
            } else {
                var copy = await context.Agent.GetAsync<ApiExtensions.MediaFireCopyFileResponse>(MediaFireApiFileMethods.Copy, new Dictionary<string, object>() {
                    { MediaFireApiParameters.QuickKey, source.Value },
                    { MediaFireApiParameters.FolderKey, destination.Value }
                });

                var newQuickKey = copy.NewQuickKeys[0];

                if (!string.IsNullOrEmpty(copyName))
                    await context.Agent.GetAsync<MediaFireEmptyResponse>(MediaFireApiFileMethods.Update, new Dictionary<string, object>() {
                        { MediaFireApiParameters.QuickKey, newQuickKey },
                        { MediaFireApiParameters.FileName, copyName }
                    });

                var item = await context.Agent.GetAsync<MediaFireGetFileInfoResponse>(MediaFireApiFileMethods.GetInfo, new Dictionary<string, object>() {
                    { MediaFireApiParameters.QuickKey, newQuickKey }
                });

                return new FileInfoContract(item.FileInfo.QuickKey, item.FileInfo.Name, item.FileInfo.Created, item.FileInfo.Created, item.FileInfo.Size, item.FileInfo.Hash);
            }
        }

        public async Task<FileSystemInfoContract> MoveItemAsync(RootName root, FileSystemId source, string moveName, DirectoryId destination, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            if (source is DirectoryId) {
                await context.Agent.GetAsync<MediaFireEmptyResponse>(MediaFireApiFolderMethods.Move, new Dictionary<string, object>() {
                    { MediaFireApiParameters.FolderKeySource, source.Value },
                    { MediaFireApiParameters.FolderKeyDestination, destination.Value }
                });

                if (!string.IsNullOrEmpty(moveName)) {
                    await context.Agent.GetAsync<MediaFireEmptyResponse>(MediaFireApiFolderMethods.Update, new Dictionary<string, object>() {
                        { MediaFireApiParameters.FolderKey, source.Value },
                        { MediaFireApiParameters.FolderName, moveName }
                    });
                }
                var item = await context.Agent.GetAsync<MediaFireGetFolderInfoResponse>(MediaFireApiFolderMethods.GetInfo, new Dictionary<string, object>() {
                    { MediaFireApiParameters.FolderKey, source.Value }
                });

                return new DirectoryInfoContract(item.FolderInfo.FolderKey, item.FolderInfo.Name, item.FolderInfo.Created, item.FolderInfo.Created);
            } else {
                var copy = await context.Agent.GetAsync<MediaFireEmptyResponse>(MediaFireApiFileMethods.Move, new Dictionary<string, object>() {
                    { MediaFireApiParameters.QuickKey, source.Value },
                    { MediaFireApiParameters.FolderKey, destination.Value }
                });

                if (!string.IsNullOrEmpty(moveName))
                    await context.Agent.GetAsync<MediaFireEmptyResponse>(MediaFireApiFileMethods.Update, new Dictionary<string, object>() {
                        { MediaFireApiParameters.QuickKey, source.Value },
                        { MediaFireApiParameters.FileName, moveName }
                    });

                var item = await context.Agent.GetAsync<MediaFireGetFileInfoResponse>(MediaFireApiFileMethods.GetInfo, new Dictionary<string, object>() {
                    { MediaFireApiParameters.QuickKey, source.Value }
                });

                return new FileInfoContract(item.FileInfo.QuickKey, item.FileInfo.Name, item.FileInfo.Created, item.FileInfo.Created, item.FileInfo.Size, item.FileInfo.Hash);
            }
        }

        public async Task<DirectoryInfoContract> NewDirectoryItemAsync(RootName root, DirectoryId parent, string name)
        {
            var context = await RequireContextAsync(root);

            var item = await context.Agent.GetAsync<MediaFireCreateFolderResponse>(MediaFireApiFolderMethods.Create, new Dictionary<string, object>() {
                { MediaFireApiParameters.ParentKey, parent.Value },
                { MediaFireApiParameters.FolderName, name }
            });

            return new DirectoryInfoContract(item.FolderKey, name, DateTimeOffset.Now, DateTimeOffset.Now);
        }

        public async Task<FileInfoContract> NewFileItemAsync(RootName root, DirectoryId parent, string name, Stream content, IProgress<ProgressValue> progress)
        {
            if (content.Length == 0)
                return new ProxyFileInfoContract(name);

            var context = await RequireContextAsync(root);

            var config = await context.Agent.Upload.GetUploadConfiguration(name, content.Length, parent.Value, MediaFireActionOnDuplicate.Skip);
            var mediaFireProgress = progress != null ? new Progress<MediaFireOperationProgress>(p => progress.Report(new ProgressValue((int)p.CurrentSize, (int)p.TotalSize))) : null;
            var upload = await context.Agent.Upload.Simple(config, content, mediaFireProgress);

            while (!(upload.IsComplete && upload.IsSuccess)) {
                await Task.Delay(100);
                upload = await context.Agent.Upload.PollUpload(upload.Key);
            }
            var item = await context.Agent.GetAsync<MediaFireGetFileInfoResponse>(MediaFireApiFileMethods.GetInfo, new Dictionary<string, object>() {
                { MediaFireApiParameters.QuickKey, upload.QuickKey }
            });

            return new FileInfoContract(item.FileInfo.QuickKey, item.FileInfo.Name, item.FileInfo.Created, item.FileInfo.Created, item.FileInfo.Size, item.FileInfo.Hash);
        }

        public async Task<bool> RemoveItemAsync(RootName root, FileSystemId target, bool recurse)
        {
            var context = await RequireContextAsync(root);

            if (target is DirectoryId) {
                await context.Agent.GetAsync<MediaFireEmptyResponse>(MediaFireApiFolderMethods.Delete, new Dictionary<string, object>() {
                    { MediaFireApiParameters.FolderKey, target.Value }
                });
            } else {
                await context.Agent.GetAsync<MediaFireEmptyResponse>(MediaFireApiFileMethods.Delete, new Dictionary<string, object>() {
                    { MediaFireApiParameters.QuickKey, target.Value }
                });
            }

            return true;
        }

        public async Task<FileSystemInfoContract> RenameItemAsync(RootName root, FileSystemId target, string newName, Func<FileSystemInfoLocator> locatorResolver)
        {
            var context = await RequireContextAsync(root);

            if (target is DirectoryId) {
                await context.Agent.GetAsync<MediaFireEmptyResponse>(MediaFireApiFolderMethods.Update, new Dictionary<string, object>() {
                    { MediaFireApiParameters.FolderKey, target.Value },
                    { MediaFireApiParameters.FolderName, newName }
                });
                var item = await context.Agent.GetAsync<MediaFireGetFolderInfoResponse>(MediaFireApiFolderMethods.GetInfo, new Dictionary<string, object>() {
                    { MediaFireApiParameters.FolderKey, target.Value }
                });

                return new DirectoryInfoContract(item.FolderInfo.FolderKey, item.FolderInfo.Name, item.FolderInfo.Created, item.FolderInfo.Created);
            } else {
                await context.Agent.GetAsync<MediaFireEmptyResponse>(MediaFireApiFileMethods.Update, new Dictionary<string, object>() {
                    { MediaFireApiParameters.QuickKey, target.Value },
                    { MediaFireApiParameters.FileName, newName }
                });
                var item = await context.Agent.GetAsync<MediaFireGetFileInfoResponse>(MediaFireApiFileMethods.GetInfo, new Dictionary<string, object>() {
                    { MediaFireApiParameters.QuickKey, target.Value }
                });

                return new FileInfoContract(item.FileInfo.QuickKey, item.FileInfo.Name, item.FileInfo.Created, item.FileInfo.Created, item.FileInfo.Size, item.FileInfo.Hash);
            }
        }

        public void PurgeSettings(RootName root)
        {
            Authenticator.PurgeRefreshToken(root?.UserName);
        }

        public void Dispose()
        {
            foreach (var context in contextCache)
                Authenticator.SaveRefreshToken(context.Key.UserName, context.Value.Agent.User.GetAuthenticationContext(), settingsPassPhrase);
            contextCache.Clear();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private static string DebuggerDisplay() => nameof(MediaFireGateway);
    }
}
