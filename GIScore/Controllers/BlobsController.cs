using GIScore.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Threading;

namespace GIScore.Controllers
{
    public class BlobsController : ApiController
    {
        private readonly IBlobService _service = new BlobService();

        [ResponseType(typeof(BlobUploadModel))]
        public async Task<IHttpActionResult> PostBlobUpload()
        {
            try
            {
                if (!Request.Content.IsMimeMultipartContent())
                {
                    return StatusCode(HttpStatusCode.UnsupportedMediaType);
                }

                var result = await _service.UploadBlobs(Request.Content);
                if (result != null)
                {
                    return Ok(result);
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }

    public interface IBlobService
    {
        Task<BlobUploadModel> UploadBlobs(HttpContent httpContent);
    }

    public class BlobService : IBlobService
    {
        public async Task<BlobUploadModel> UploadBlobs(HttpContent httpContent)
        {
            var blobUploadProvider = new BlobStorageUploadProvider();
            var result = await httpContent.ReadAsMultipartAsync(blobUploadProvider);
            return result.Uploads;
        }
    }

    public static class BlobHelper
    {
        public static CloudBlobContainer GetBlobContainer()
        {
            var blobStorageConnectionString = ConfigurationManager.AppSettings["BlobStorageConnectionString"];
            var blobContainerName = ConfigurationManager.AppSettings["BlobStorageContainerName"];

            var blobStorageAccount = CloudStorageAccount.Parse(blobStorageConnectionString);
            var blobClient = blobStorageAccount.CreateCloudBlobClient();

            return blobClient.GetContainerReference(blobContainerName);
        }
    }

    public class BlobStorageUploadProvider : MultipartFileStreamProvider
    {
        public BlobUploadModel Uploads { get; set; }

        public BlobStorageUploadProvider() : base(Path.GetTempPath())
        {
            Uploads = new BlobUploadModel();
        }

        public override Task ExecutePostProcessingAsync()
        {
            
            foreach(var fileData in FileData)
            {
                var fileName = Path.GetFileName(fileData.Headers.ContentDisposition.FileName.Trim('"'));
                var blobContainer = BlobHelper.GetBlobContainer();
                var blob = blobContainer.GetBlockBlobReference(fileName);

                //blob.Properties.ContentType = fileData.Headers.ContentType.MediaType;

                using (var fs = File.OpenRead(fileData.LocalFileName))
                {
                    blob.UploadFromStream(fs);
                }

                File.Delete(fileData.LocalFileName);

                var blobUpload = new BlobUploadModel
                {
                    FileName = blob.Name,
                    FileUrl = blob.Uri.AbsoluteUri,
                    FileSizeBt = blob.Properties.Length
                };

                Uploads = blobUpload;
            }
            return base.ExecutePostProcessingAsync();
        }
    }
}
