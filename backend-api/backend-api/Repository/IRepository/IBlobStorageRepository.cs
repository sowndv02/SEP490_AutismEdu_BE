namespace backend_api.Repository.IRepository
{
    public interface IBlobStorageRepository
    {
        Task<string> UploadImg(Stream data, string filename, bool isPrivate = false);

        string GetBlobSasUrl(string? blobUrl);
    }
}
