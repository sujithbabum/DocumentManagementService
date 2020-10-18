using System;
using System.Collections.Generic;
using System.IO;
namespace DocumentManagementService.Models
{
    /// <summary>
    /// The File class 
    /// </summary>
    public class DocumentDetails
    {
        /// <summary>
        /// Gets or sets the Document Name
        /// </summary>
        public string DocumentName { get; set; }

        /// <summary>
        /// Gets or sets the contentType 
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        ///  Gets or sets Document Length
        /// </summary>
        public long? DocumentLength { get; set; }

        /// <summary>
        /// Gets or sets the file stream
        /// </summary>
        public byte[] Content { get; set; }
    }
}
