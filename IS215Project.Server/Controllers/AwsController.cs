using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Mvc;

namespace IS215Project.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AwsController : ControllerBase
    {
        private readonly IAmazonS3 _client = new AmazonS3Client();

        [HttpGet]
        public bool TestConnection() => true;

        [HttpGet]
        public async Task<List<S3Bucket>> GetBucketsAsync()
        {
            var response = await _client.ListBucketsAsync();

            return response.Buckets;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateContentFromImageAsync([FromForm] IFormFile file)
        {
            await UploadImageToS3(file);

            GetResponseFromLambda();

            return new JsonResult("Lorem Ipsum ...");
        }

        private async Task UploadImageToS3(IFormFile file)
        {
            // 1. Call S3 to Upload Image
            var transfer = new TransferUtility(_client);

            await using var stream = file.OpenReadStream();

            // TODO get bucket name from config?
            await transfer.UploadAsync(
                stream,
                "is215-project-group2",
                GetFilenameWithTimestamp(file.FileName)
            );
        }

        private void GetResponseFromLambda()
        {
            // TODO
            // 2. Return Lambda Response
        }

        private string GetFilenameWithTimestamp(string filename)
        {
            // Make filename unique
            var baseName = Path.GetFileNameWithoutExtension(filename);
            var ext = Path.GetExtension(filename);

            return $"{baseName}.{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        }
    }
}
