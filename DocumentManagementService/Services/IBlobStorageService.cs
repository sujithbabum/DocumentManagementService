namespace DocumentManagementService.Services
{
    using Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Blob Storage Service
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>
        /// Uploads a document to Azure Blob Storage 
        /// </summary>        
        /// <param name="document"></param>        
        /// <returns>document path</returns>
        Task<string> UploadDocumentBlob(DocumentDetails document);

        /// <summary>
        /// Checks if the document blob already exists
        /// </summary>
        /// <param name="documentName">document name</param>
        /// <returns>true or false</returns>
        bool CheckIfDocumentBlobExists(string documentName);

        /// <summary>
        /// Gets document from Blob Storage
        /// </summary>
        /// <param name="documentName">document name</param>
        /// <returns>the document</returns>
        Task<DocumentDetails> GetDocumentBlob(string documentName);

        /// <summary>
        /// Get the list of documents  
        /// </summary>
        /// <returns>the list of documents</returns>
        Task<IEnumerable<DocumentDetails>> ListDocumentBlobs();

        /// <summary>
        /// Deletes the document from Blob Storage
        /// </summary>
        /// <param name="documentName">document name</param>
        /// <returns>true or false</returns>
        Task<bool> DeleteDocumentBlob(string documentName);

    }
}
