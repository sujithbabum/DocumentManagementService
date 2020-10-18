namespace DocumentManagementService.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Models;
    using Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// The Document API Controller
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        /// <summary>
        /// The Logger 
        /// </summary>
        private readonly ILogger<DocumentController> _logger;

        /// <summary>
        /// Blob Storage Service
        /// </summary>
        private readonly IBlobStorageService _blobStorageService;

        /// <summary>
        /// Blob Storage config 
        /// </summary>
        private readonly IOptions<DocumentConfig> _documentConfig;

        /// <summary>
        /// Creates a new instance of <see cref="DocumentController"/> API 
        /// </summary>
        /// <param name="blobStorageService"></param>
        /// <param name="logger"></param>
        /// <param name="documentConfig"></param>
        public DocumentController(IBlobStorageService blobStorageService, ILogger<DocumentController> logger, IOptions<DocumentConfig> documentConfig)
        {
            _blobStorageService = blobStorageService;
            _logger = logger;
            _documentConfig = documentConfig;
        }

        /// <summary>
        /// Http Post method to upload pdf document to Azure Blob Storage
        /// </summary>
        /// <param name="documentFile">document to upload</param>
        /// <returns>uploaded document Absolute URI</returns>
        [Route("upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile documentFile)
        {
            try
            {
                ValidateDocument(documentFile);

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var documentDetails = new DocumentDetails()
                {
                    DocumentName = Path.GetFileName(documentFile.FileName),
                    DocumentLength = documentFile.Length,
                    ContentType = documentFile.ContentType
                };

                await using (var target = new MemoryStream())
                {
                    await documentFile.CopyToAsync(target);
                    documentDetails.Content = target.ToArray();
                }

                var documentLocation = await _blobStorageService.UploadDocumentBlob(documentDetails);

                return Ok(documentLocation);
            }
            catch (Exception ex)
            {
                var errorMessage = $"failed to upload document : {documentFile.FileName} ";
                _logger.LogError(ex, errorMessage);
                return BadRequest(errorMessage);
            }
        }

        /// <summary>
        /// Http Get Method for document request
        /// </summary>
        /// <returns>requested document if exists</returns>
        [ProducesResponseType(typeof(FileContentResult),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("download/{documentName}")]
        public async Task<IActionResult> DownloadDocument(string documentName)
        {
            try
            {
                if (string.IsNullOrEmpty(documentName))
                    return BadRequest("Please provide a document name");

                if (_blobStorageService.CheckIfDocumentBlobExists(documentName))
                {
                    var documentDetails = await _blobStorageService.GetDocumentBlob(documentName);

                    return new FileContentResult(documentDetails.Content, documentDetails.ContentType);
                }
                else
                {
                    return BadRequest("Requested document Doesn't exist");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error downloading document:  {documentName}";
                _logger.LogError(ex, errorMessage);
                return BadRequest(errorMessage);
            }
        }

        /// <summary>
        /// Returns the list of documents stored in blob storage.
        /// </summary>
        [HttpGet]
        [Route("documentsList")]
        [ProducesResponseType(typeof(IEnumerable<DocumentDetails>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDocumentsList()
        {
            try
            {
                return Ok(await _blobStorageService.ListDocumentBlobs());
            }
            catch (Exception ex)
            {
                var errorMessage = "Error getting documents list";
                _logger.LogError(ex,$"Message: {errorMessage}");
                return BadRequest(errorMessage);
            }
        }

        /// <summary>
        /// Deletes a document from the blob storage 
        /// </summary>
        /// <param name="documentName">document name</param>
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete("delete/{documentName}")]
        public async Task<IActionResult> DeleteDocument(string documentName)
        {
            try
            {
                if (string.IsNullOrEmpty(documentName))
                    return BadRequest("Document name not provided");

                if (_blobStorageService.CheckIfDocumentBlobExists(documentName))
                {
                    if (await _blobStorageService.DeleteDocumentBlob(documentName))
                        return Ok($"Document : {documentName} deleted successfully");
                    else
                        return BadRequest($"Unable to delete document : {documentName}");                    
                }
                else
                {
                    return BadRequest("Document doesn't exist");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"failed to delete document : {documentName} ";
                _logger.LogError(ex, errorMessage);
                return BadRequest(errorMessage);
            }
        }

        /// <summary>
        /// document validation
        /// </summary>
        /// <param name="documentFile"></param>
        private void ValidateDocument(IFormFile documentFile)
        {
            if (documentFile != null)
            {
                if (documentFile.Length > _documentConfig.Value.MaxDocumentSizeAllowed)
                    ModelState.AddModelError("DocumentSizeExceeded",
                                             $"Document size is bigger than maximum allowed document size {_documentConfig.Value.MaxDocumentSizeAllowed}");

                if (!_documentConfig.Value.SupportedTypes.Contains(documentFile.ContentType))
                    ModelState.AddModelError("InvalidDocumentType", $"Uploaded Document type is not supported");
            }
            else
            {
                ModelState.AddModelError("NoDocument", "Document not uploaded");
            }
        }
    }
}
