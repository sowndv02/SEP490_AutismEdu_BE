using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using backend_api.Repository.IRepository;

namespace backend_api.Repository
{
    public class BlobStorageRepository : IBlobStorageRepository
    {
        private string ConnectionString = string.Empty;
        private string LogosContainerName_Private = string.Empty;
        private string LogosContainerName_Public = string.Empty;
        private string AccountKey = string.Empty;
        public BlobStorageRepository(IConfiguration configuration)
        {
            ConnectionString = configuration.GetValue<string>("BlobStorage:ConnectionString");
            LogosContainerName_Private = configuration.GetValue<string>("BlobStorage:LogosContainerName_Private");
            LogosContainerName_Public = configuration.GetValue<string>("BlobStorage:LogosContainerName_Public");
            AccountKey = configuration.GetValue<string>("BlobStorage:AccountKey");
        }

        public string GetBlobSasUrl(string? blobUrl)
        {
            try
            {
                if (blobUrl == null) return null;
                var sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = LogosContainerName_Private,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow,
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30),
                    BlobName = GetBlobNameFromUrl(blobUrl)
                };
                var blobServiceClient = new BlobServiceClient(ConnectionString);
                sasBuilder.SetPermissions(BlobSasPermissions.Read);
                var sasToken = sasBuilder.ToSasQueryParameters(new Azure.Storage.StorageSharedKeyCredential(blobServiceClient.AccountName, AccountKey)).ToString();
                return $"{blobUrl}?{sasToken}";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private string GetBlobNameFromUrl(string blobUrl)
        {
            var uri = new Uri(blobUrl);
            return uri.Segments.Last();
        }

        public async Task<string> UploadImg(Stream data, string fileName, bool isPrivate = false)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(ConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(LogosContainerName_Public);

                if (isPrivate)
                    containerClient = blobServiceClient.GetBlobContainerClient(LogosContainerName_Private);

                var blobClient = containerClient.GetBlobClient(fileName);
                await blobClient.UploadAsync(data);
                var blobUrl = blobClient.Uri.ToString();
                return blobUrl;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
