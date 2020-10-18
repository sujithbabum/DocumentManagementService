namespace DocumentManagementService.Models
{
    /// <summary>
    /// Blob Storage Config class
    /// </summary>
    public class BlobStorageConfig
    {
        /// <summary>
        /// Gets or sets Azure blob storage Connectionstring from appsettings.json file
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// gets or sets Containername from appsettings.json file
        /// </summary>
        public string ContainerName { get; set; }
    }
}
