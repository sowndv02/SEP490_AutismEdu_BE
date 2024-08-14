using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class BlobStorageRepository : IBlobStorageRepository
    {
        private string ConnectionString = string.Empty;
        private string LogosContainerName = string.Empty;
        private string AccountKey = string.Empty;
        public BlobStorageRepository(IConfiguration configuration)
        {
            ConnectionString = configuration.GetValue<string>("BlobStorage:ConnectionString");
            LogosContainerName = configuration.GetValue<string>("BlobStorage:LogosContainerName");
            AccountKey = configuration.GetValue<string>("BlobStorage:AccountKey");
        }

        public string GetBlobSasUrl(string? blobUrl)
        {
            if (blobUrl == null) return null;
            var sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = LogosContainerName,
                Resource = "b",
                StartsOn = DateTime.Now,
                ExpiresOn = DateTime.Now.AddMinutes(30),
                BlobName = GetBlobNameFromUrl(blobUrl)
            };
            var blobServiceClient = new BlobServiceClient(ConnectionString);
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sasToken = sasBuilder.ToSasQueryParameters(new Azure.Storage.StorageSharedKeyCredential(blobServiceClient.AccountName, AccountKey)).ToString();
            return $"{blobUrl}?{sasToken}";
        }

        private string GetBlobNameFromUrl(string blobUrl)
        {
            var uri = new Uri(blobUrl);
            return uri.Segments.Last();
        }

        public async Task<string> UploadImg(Stream data, string fileName)
        {
            var blobServiceClient = new BlobServiceClient(ConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(LogosContainerName);

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(data);
            var blobUrl = blobClient.Uri.ToString();
            return blobUrl;
        }
    }
}
