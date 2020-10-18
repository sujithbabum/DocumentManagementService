namespace DocumentManagementService.Common
{
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;

    /// <summary>
    /// Azure Blob Container Client wrapper
    /// </summary>
    public interface IBlobContainerWrapper
    {
        /// <summary>
        /// creates blob container client if not already exists
        /// </summary>
        /// <returns></returns>
        Task<BlobContainerClient> CreateBlobContainerClient();
    }
}
