using DocumentManagementService.Controllers;
using DocumentManagementService.Models;
using DocumentManagementService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DocumentManagementService.Tests
{
    /// <summary>
    /// The Document Controller Test class
    /// </summary>
    public class DocumentControllerTests
    {
        /// <summary>
        /// The document config options
        /// </summary>
        private Mock<IOptions<DocumentConfig>> _mockDocumentConfigOptions;

        /// <summary>
        /// The mock logger 
        /// </summary>
        private readonly Mock<ILogger<DocumentController>> _mockLogger;

        /// <summary>
        /// The document controller
        /// </summary>
        private DocumentController _documentController;

        /// <summary>
        /// Mock Blob Service
        /// </summary>
        private readonly Mock<IBlobStorageService> _mockBlobService;

        /// <summary>
        /// initialises a new instance of <see cref="DocumentControllerTests"/> and set up tests 
        /// </summary>
        public DocumentControllerTests()
        {
            _mockLogger = new Mock<ILogger<DocumentController>>();
            SetUpDocumentConfig();
            _mockBlobService = new Mock<IBlobStorageService>();
        }

        /// <summary>
        /// Tests UploadDocument Post method return Ok response for valid document
        /// </summary>
        [Fact]
        public async Task UploadDocument_PDF_ReturnsOK()
        {
            // Arrange 
            IFormFile document = CreateTestFormFile("Test.pdf", "Test Content", "application/pdf", 2 * 1024 * 1024);
            var uri = $"http://WindowsAzure.co.uk/test.pdf";
            _mockBlobService.Setup(x => x.UploadDocumentBlob(It.IsAny<DocumentDetails>())).Returns(() => Task.FromResult(uri));
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.UploadDocument(document);
            var expected = uri;

            // Assert 
            Assert.IsType<OkObjectResult>(actual);
            Assert.Equal(expected, ((OkObjectResult)actual).Value);
        }

        /// <summary>
        /// Tests Upload Document returns Bad Request for invalid document type
        /// </summary>
        [Fact]
        public async Task UploadDocument_NonPDF_ReturnsBadRequest()
        {
            // Arrange 
            IFormFile document = CreateTestFormFile("Test.json", "Test Content", "application/json", 10 * 1024);
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.UploadDocument(document);
            var actualErrorString = ((SerializableError)(((BadRequestObjectResult)actual).Value)).GetValueOrDefault("InvalidDocumentType");
            var expectedErrorString = "Uploaded Document type is not supported";


            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expectedErrorString, ((string[])actualErrorString)?[0]);
        }

        /// <summary>
        /// Tests Upload Document returns Bad Request for invalid document type
        /// </summary>
        [Fact]
        public async Task UploadDocument_DocumentLargerThanAllowedSize_ReturnsBadRequest()
        {
            // Arrange 
            IFormFile document = CreateTestFormFile("Test.pdf", "Test Content", "application/json", 1024 * 1024 * 1024);
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.UploadDocument(document);
            var actualErrorString = ((SerializableError)(((BadRequestObjectResult)actual).Value)).GetValueOrDefault("DocumentSizeExceeded");
            var expectedErrorString = "Document size is bigger than maximum allowed document size 5242880";

            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expectedErrorString, ((string[])actualErrorString)?[0]);
        }

        /// <summary>
        /// Tests Upload Document returns Bad Request for invalid document type
        /// </summary>
        [Fact]
        public async Task UploadFile_BlobServiceThrowException_ReturnsBadRequest()
        {
            // Arrange 
            IFormFile document = CreateTestFormFile("Test.pdf", "Test Content", "application/pdf", 2 * 1024 * 1024);
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);
            _mockBlobService.Setup(x => x.UploadDocumentBlob(It.IsAny<DocumentDetails>())).Throws(new UnauthorizedAccessException("Not Authorised"));
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.UploadDocument(document);
            var expected = "failed to upload document : Test.pdf ";

            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        /// <summary>
        /// Tests document download is successful. 
        /// </summary>
        [Fact]
        public async Task GetDocument_Returns_Document()
        {
            // Arrange 
            var documentName = "Test.pdf";
            var contentType = "application/pdf";
            var bytes = Encoding.ASCII.GetBytes("This is test document string");
            var documentContent = new FileContentResult(bytes, contentType) {FileDownloadName = documentName};
            var documentDetails = new DocumentDetails()
            {
                DocumentName = documentName,
                ContentType = contentType,
                Content = documentContent.FileContents
            };
            _mockBlobService.Setup(x => x.CheckIfDocumentBlobExists(It.IsAny<string>())).Returns(true);
            _mockBlobService.Setup(x => x.GetDocumentBlob(It.IsAny<string>())).Returns(() => Task.FromResult(documentDetails));            
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);
                       

            // Act
            var actual = await _documentController.DownloadDocument(documentName) as FileContentResult;
            var expected = documentContent;

            // Assert 
            Assert.IsType<FileContentResult>(actual);
            Assert.Equal(expected.ContentType, actual.ContentType);            
        }

        /// <summary>
        /// Tests DownloadDocument returns Bad Request when no document name provided
        /// </summary>
        [Fact]
        public async Task GetDocument_WithNoDocumentName_Returns_BadRequest()
        {
           // Arrange
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.DownloadDocument("");
            var expected = "Please provide a document name";

            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        /// <summary>
        /// Tests DownloadDocument returns Bad Request when document doesn't exist
        /// </summary>
        [Fact]
        public async Task GetDocument_DocumentDoesntExist_Return_BadRequest()
        {
            // Arrange
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.DownloadDocument("Test.pdf");
            var expected = "Requested document Doesn't exist";

            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        /// <summary>
        /// Tests DownloadDocument returns Bad Request when document doesn't exist
        /// </summary>
        [Fact]
        public async Task GetDocument_ThrowsException_Return_BadRequest()
        {
            _mockBlobService.Setup(x => x.CheckIfDocumentBlobExists(It.IsAny<string>())).Throws(new UnauthorizedAccessException("Test Exception"));
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.DownloadDocument("Test.pdf");
            var expected = "Error downloading document:  Test.pdf";

            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        /// <summary>
        /// Tests GetDocumentsList returns list of documents
        /// </summary>
        [Fact]
        public async Task GetFileList_ReturnOk()
        {
            IEnumerable<DocumentDetails> documents = new List<DocumentDetails>();
            _mockBlobService.Setup(x => x.ListDocumentBlobs()).Returns(() => Task.FromResult(documents));
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.GetDocumentsList();            

            // Assert 
            Assert.IsType<OkObjectResult>(actual);            
        }

        /// <summary>
        /// Tests GetDocumentsList returns Bad Request
        /// </summary>
        [Fact]
        public async Task GetDocumentsList_ThrowsException_Returns_BadRequest()
        {
            _mockBlobService.Setup(x => x.ListDocumentBlobs()).Throws(new UnauthorizedAccessException("Test Exception"));
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.GetDocumentsList();
            var expected = "Error getting documents list";

            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        /// <summary>
        /// Tests Delete document is successful.
        /// </summary>
        [Fact]
        public async Task DeleteDocument_Returns_Success()
        {
            // Arrange            
            _mockBlobService.Setup(x => x.CheckIfDocumentBlobExists(It.IsAny<string>())).Returns(true);
            _mockBlobService.Setup(x => x.DeleteDocumentBlob(It.IsAny<string>())).Returns(() => Task.FromResult(true));
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);


            // Act
            var actual = await _documentController.DeleteDocument("Test.pdf");            

            // Assert 
            Assert.IsType<OkObjectResult>(actual);
        }

        /// <summary>
        /// Tests DeleteDocument returns Bad Request when no document name provided
        /// </summary>
        [Fact]
        public async Task DeleteDocument_WithNoDocumentName_Returns_BadRequest()
        {
            // Arrange
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.DeleteDocument("");
            var expected = "Document name not provided";

            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        /// <summary>
        /// Test DeleteDocument returns Bad Request when document doesn't exist
        /// </summary>
        [Fact]
        public async Task DeleteDocument_DocumentDoesntExist_Returns_BadRequest()
        {
            // Arrange
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.DeleteDocument("Test.pdf");
            var expected = "Document doesn't exist";

            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        /// <summary>
        /// Tests Delete document when delete failed
        /// </summary>
        [Fact]
        public async Task DeleteDocument_WhenDeleteFailed_Returns_BadRequest()
        {
            // Arrange            
            _mockBlobService.Setup(x => x.CheckIfDocumentBlobExists(It.IsAny<string>())).Returns(true);
            _mockBlobService.Setup(x => x.DeleteDocumentBlob(It.IsAny<string>())).Returns(() => Task.FromResult(false));
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);


            // Act
            var actual = await _documentController.DeleteDocument("Test.pdf");
            var expected = "Unable to delete document : Test.pdf";

            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        /// <summary>
        /// Test DeleteDocument returns Bad Request when document doesn't exist
        /// </summary>
        [Fact]
        public async Task DeleteDocument_ThrowsException_Returns_BadRequest()
        {
            _mockBlobService.Setup(x => x.CheckIfDocumentBlobExists(It.IsAny<string>())).Throws(new UnauthorizedAccessException("Test Exception"));
            _documentController = new DocumentController(_mockBlobService.Object, _mockLogger.Object, _mockDocumentConfigOptions.Object);

            // Act
            var actual = await _documentController.DeleteDocument("Test.pdf");
            var expected = "failed to delete document : Test.pdf ";

            // Assert 
            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        #region "Setup Tests"

        /// <summary>
        /// Sets up Document Config 
        /// </summary>
        private void SetUpDocumentConfig()
        {
            _mockDocumentConfigOptions = new Mock<IOptions<DocumentConfig>>();

            DocumentConfig config = new DocumentConfig()
            {
                MaxDocumentSizeAllowed = 5242880,
                SupportedTypes = new[] { "application/pdf" }
            };
            _mockDocumentConfigOptions.Setup(x => x.Value).Returns(config);            
        }

        /// <summary>
        /// Creates IFormFile
        /// </summary>
        /// <param name="documentName">document name</param>
        /// <param name="content">content</param>
        /// <param name="contentType">content type</param>
        /// <param name="documentLength">document length</param>
        /// <returns></returns>
        private IFormFile CreateTestFormFile(string documentName, string content, string contentType, long documentLength)
        {
            var documentBytes = Encoding.UTF8.GetBytes(content);

            var document = new FormFile(new MemoryStream(documentBytes), 0, documentLength, null, documentName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            return document;
        }

        #endregion
    }
}
