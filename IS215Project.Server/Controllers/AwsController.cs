using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Mvc;

namespace IS215Project.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AwsController(IConfiguration config) : ControllerBase
    {
        //private readonly IAmazonS3 _client;
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
            var filenameWithTimestamp = await UploadImageToS3(file);

            // Get the generated content from Output Bucket
            // GetResponseFromLambda();

            var expectedFilename = GetExpectedOutputFilename(filenameWithTimestamp);
            var content = await GetGeneratedContentAsync(expectedFilename);

            return content;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImageAsync([FromForm] IFormFile file)
        {
            var filenameWithTimestamp = await UploadImageToS3(file);

            return new JsonResult(filenameWithTimestamp);
        }

        [HttpGet]
        public async Task<IActionResult> GetGeneratedContentAsync(string filename)
        {
            //var content =
            //    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Pellentesque placerat nunc nec leo finibus, at porta sapien commodo. Morbi dictum ante velit, quis fringilla urna finibus nec. Ut consectetur congue purus at feugiat. Nulla sed scelerisque elit, quis sagittis massa. Aenean risus turpis, tempor at velit nec, porttitor consectetur est. Mauris sed quam in lectus tempor venenatis. Nam suscipit accumsan ipsum ut ornare. Nunc commodo dui at nisl efficitur interdum. Quisque id tellus ullamcorper, feugiat arcu in, accumsan mauris. Phasellus risus metus, venenatis fringilla velit volutpat, porta lacinia enim. Suspendisse eget lectus ac turpis feugiat lobortis. Ut pulvinar eu purus nec pharetra. Etiam turpis turpis, finibus non tellus eu, molestie consequat ipsum.";
            await RefreshBucketAsync(GetOutputBucketName());

            using var response = await _client.GetObjectAsync(
                GetOutputBucketName(),
                filename);

            using var reader = new StreamReader(response.ResponseStream);

            var content = await reader.ReadToEndAsync();

            return new JsonResult(content);
        }

        // Refresh the output bucket name
        private async Task RefreshBucketAsync(string bucketName)
        {
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                };

                ListObjectsV2Response response;
                do
                {
                    response = await _client.ListObjectsV2Async(request);
                    foreach (S3Object entry in response.S3Objects)
                    {
                        System.Console.WriteLine($"Object key: {entry.Key}");
                    }
                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated);
            }
            catch (AmazonS3Exception e)
            {
                System.Console.WriteLine("Error encountered on server. Message:'{0}' when listing objects", e.Message);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Unknown encountered on server. Message:'{0}' when listing objects", e.Message);
            }
        }

        private async Task<string> UploadImageToS3(IFormFile file)
        {
            var filename = GetFilenameWithTimestamp(file.FileName);

            // 1. Call S3 to Upload Image
            using var transfer = new TransferUtility(_client);

            await using var stream = file.OpenReadStream();

            await transfer.UploadAsync(
                stream,
                GetInputBucketName(),
                filename
            );

            return filename;
        }

        private void GetResponseFromLambda()
        {
            // TODO
            // 2. Return Lambda Response
        }

        private string GetInputBucketName()
        {
            // Get bucket name from appsettings.json
            var bucketName = config.GetValue<string>("AwsContext:S3BucketInputName");

            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException(nameof(bucketName), "AwsContext:S3BucketInputName is null or invalid.");

            return bucketName;
        }

        private string GetOutputBucketName()
        {
            // Get bucket name from appsettings.json
            var bucketName = config.GetValue<string>("AwsContext:S3BucketOutputName");

            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException(nameof(bucketName), "AwsContext:S3BucketOutputName is null or invalid.");

            return bucketName;
        }

        private string GetFilenameWithTimestamp(string filename)
        {
            // Make filename unique
            var baseName = Path.GetFileNameWithoutExtension(filename);
            var ext = Path.GetExtension(filename);

            return $"{baseName}.{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        }

        private string GetExpectedOutputFilename(string filenameWithTimestamp)
        {
            // Return expected output filename
            var pos = filenameWithTimestamp.LastIndexOf(".");
            return filenameWithTimestamp.Substring(0, pos < 0 ? filenameWithTimestamp.Length : pos) + ".txt";
        }
    }
}
