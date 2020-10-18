namespace DocumentManagementService.Models
{
    /// <summary>
    /// Document Configuration
    /// </summary>
    public class DocumentConfig
    {
        /// <summary>
        /// BlobStorage connection settings
        /// </summary>
        public BlobStorageConfig BlobStorageConfig { get; set; }   

        /// <summary>
        /// Gets or sets max document size
        /// </summary>
        public int MaxDocumentSizeAllowed { get; set; }

        /// <summary>
        /// Gets or sets Supported document types
        /// </summary>
        public string[] SupportedTypes { get; set; }        
    }
}
