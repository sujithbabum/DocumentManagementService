namespace DocumentManagementService.Common
{
    using System.Threading.Tasks;
    using Models;
    using Azure.Storage.Blobs;
    using Microsoft.Extensions.Options;

    /// <inheritdoc />
    public class BlobContainerWrapper : IBlobContainerWrapper
    {
        /// <summary>
        ///  document config settings
        /// </summary>
        private readonly IOptions<DocumentConfig> _documentConfig;

        public BlobContainerWrapper(IOptions<DocumentConfig> documentConfig)
        {
            _documentConfig = documentConfig;
        }

        /// <inheritdoc />
        public async Task<BlobContainerClient> CreateBlobContainerClient()
        {
            var blobContainerClient = new BlobContainerClient(_documentConfig.Value.BlobStorageConfig.ConnectionString, _documentConfig.Value.BlobStorageConfig.ContainerName);
            await blobContainerClient.CreateIfNotExistsAsync();
            return blobContainerClient;
        }
    }
}
