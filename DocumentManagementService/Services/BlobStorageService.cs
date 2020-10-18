namespace DocumentManagementService.Services
{
    using Azure.Storage.Blobs;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs.Models;
    using System.IO;
    using Helpers;
    using Microsoft.Extensions.Logging;
    using Common;

    /// <inheritdoc />
    public class BlobStorageService : IBlobStorageService
    {
        /// <summary>
        /// The Logger 
        /// </summary>
        private readonly ILogger<BlobStorageService> _logger;

        /// <summary>
        /// Blob Container client
        /// </summary>
        private readonly BlobContainerClient _blobContainerClient;

        /// <summary>
        /// Creates a new instance of <see cref="BlobStorageService"/>
        /// </summary>        
        /// <param name="blobContainerWrapper"></param>
        /// <param name="logger"></param>
        public BlobStorageService(IBlobContainerWrapper blobContainerWrapper, ILogger<BlobStorageService> logger)
        {
            try
            {
                _logger = logger;
                _blobContainerClient = blobContainerWrapper.CreateBlobContainerClient().Result;
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                _logger.LogError(ex, "Error creating a new blob container");
                throw;
            }
        }

        /// <inheritdoc />
        public bool CheckIfDocumentBlobExists(string documentName)
        {
            var documentExists = false;
            try
            {                
                var blobClient = _blobContainerClient.GetBlobClient(documentName);
                if (blobClient != null)
                {
                    documentExists = true;
                }
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                _logger.LogError(ex, "Error getting the document blob");
                throw;
            }
            return documentExists;
        }

        /// <inheritdoc />
        public async Task<DocumentDetails> GetDocumentBlob(string documentName)
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(documentName);
                var blobDownloadInfo = await blobClient.DownloadAsync();

                var document = new DocumentDetails()
                {
                    DocumentName = documentName,
                    Content = DocumentHelper.ReadContent(blobDownloadInfo.Value.Content),
                    ContentType = blobDownloadInfo.Value.ContentType
                };

                return document;
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                _logger.LogError(ex, "Error getting the document blob");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DocumentDetails>> ListDocumentBlobs()
        {
            try
            {
                var documents = new List<DocumentDetails>();

                await foreach (var blobItem in _blobContainerClient.GetBlobsAsync())
                {
                    documents.Add(new DocumentDetails()
                    {
                        DocumentName = blobItem.Name,
                        ContentType = blobItem.Properties.ContentType,
                        DocumentLength = blobItem.Properties.ContentLength
                    });
                }

                return documents;
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                _logger.LogError(ex, "Error getting blob items");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string> UploadDocumentBlob(DocumentDetails document)
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(document.DocumentName);

                await blobClient.UploadAsync(new MemoryStream(document.Content), new BlobHttpHeaders { ContentType = document.ContentType });
                return blobClient.Uri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                _logger.LogError(ex, "Error uploading the document blob");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteDocumentBlob(string documentName)
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(documentName);
                return await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                _logger.LogError(ex, "Error deleting the document blob");
                throw;
            }            
        }
    }
}
