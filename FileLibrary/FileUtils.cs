using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;
using System.IO;

namespace FileLibrary
{
    public class FileUtils
    {
        private GraphServiceClient _graphClient;
        private string _clientID;
        private string _clientSecret;
        private string _upn;
        private string _folderPath;
        private string _tenantId;
        private int _sizeMultiple;
        public FileUtils(string clientID, string clientSecret, string tenantId, string folderPath, string Upn, int sizeMultiple)
        {
            _clientID = clientID;
            _clientSecret = clientSecret;
            TenantId = tenantId;
            _folderPath = folderPath;
            _upn = Upn;
            _sizeMultiple = sizeMultiple;
        }

        public GraphServiceClient graphClient { get => _graphClient; set => _graphClient = value; }
        public string ClientId { get => _clientID; set => _clientID = value; }
        public string ClientSecret { get => _clientSecret; set => _clientSecret = value; }
        public string UPN { get => _upn; set => _upn = value; }
        public string FolderPath { get => _folderPath; set => _folderPath = value; }
        public string TenantId { get => _tenantId; set => _tenantId = value; }
        public int SizeMultiple { get => _sizeMultiple; set => _sizeMultiple = value; }

        private void createClient()
        {
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                TenantId, ClientId, ClientSecret, options);

            graphClient = new GraphServiceClient(clientSecretCredential, scopes);

        }

        public async Task Upload()
        {
            try
            {
                FileAttributes attr = File.GetAttributes(FolderPath);
                createClient();
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    await UploadFolderAsync(FolderPath);
                }
                else
                {
                   await UploadFileAsync(FolderPath);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading: {ex.ToString()}");
            }
        }
        private List<String> dirSearch(string sDir)
        {
            List<String> files = new List<String>();
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    files.Add(f);
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    files.AddRange(dirSearch(d));
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error uploading: {ex.ToString()}");
            }

            return files;
        }
        public async Task UploadFolderAsync(string folder)
        {
            var tasks = new List<Task>();
            
            var files = dirSearch(folder);
            foreach (var f in files)
            {
               await UploadFileAsync(f);
            }
            //await Task.WhenAll(tasks);
        }

        public async Task UploadFileAsync(string pathFile)
        {
            
            using var fileStream = System.IO.File.OpenRead(pathFile);

            // Use properties to specify the conflict behavior
            // in this case, replace
            var uploadSessionRequestBody = new CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", "replace" }
                    }
                }
            };
            var driveItem = await graphClient.Users[UPN].Drive.GetAsync();
            // Create the upload session
            // itemPath does not need to be a path to an existing item
            var uploadSession = await graphClient.Drives[driveItem.Id].Root
                .ItemWithPath(Path.GetFileName( Path.GetFileName( fileStream.Name)))
                .CreateUploadSession
                .PostAsync(uploadSessionRequestBody);

            // Max slice size must be a multiple of 320 KiB
            int maxSliceSize = 320 * SizeMultiple;
            var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, maxSliceSize, graphClient.RequestAdapter);

            var totalLength = fileStream.Length;
            // Create a callback that is invoked after each slice is uploaded
            IProgress<long> progress = new Progress<long>(prog =>
            {
                Console.WriteLine($"{Path.GetFileName(fileStream.Name)} Uploaded {prog} bytes of {totalLength} bytes {(prog/1.0/totalLength)*100} %");
            });

            try
            {
                // Upload the file
                var uploadResult = await fileUploadTask.UploadAsync(progress);

                Console.WriteLine(uploadResult.UploadSucceeded ?
                    $"Upload complete, item ID: {uploadResult.ItemResponse.Id}" :
                    "Upload failed");
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error uploading: {ex.ToString()}");
            }
        }

    }
}