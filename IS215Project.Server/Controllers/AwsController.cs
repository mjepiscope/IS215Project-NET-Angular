using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using IS215Project.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;

namespace IS215Project.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AwsController(IConfiguration config) : ControllerBase
    {
        private readonly IAmazonS3 _s3 = new AmazonS3Client();
        private readonly IAmazonDynamoDB _dynamo = new AmazonDynamoDBClient();
        private const string TableName = "Article";

        [HttpGet]
        public bool TestConnection() => true;

        [HttpGet]
        public async Task<List<S3Bucket>> GetBucketsAsync()
        {
            var response = await _s3.ListBucketsAsync();

            return response.Buckets;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImageAsync([FromForm] IFormFile file)
        {
            var response = new UploadImageResponse { IsSuccess = false };

            if (!await IsImageValidAsync(file))
            {
                response.ErrorMessage = $"File {file.FileName} is invalid.";

                return new JsonResult(response);
            }

            response.Timestamp = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            response.ImageFilename = GetFilenameWithTimestamp(file.FileName, response.Timestamp);

            if (!await InsertItemToDynamo(response.Timestamp, response.ImageFilename))
            {
                response.ErrorMessage = "Failed to insert item to DynamoDB.";
                
                return new JsonResult(response);
            }

            if (!await UploadImageToS3(file, response.ImageFilename))
            {
                response.ErrorMessage = "Failed to upload image to S3 Bucket.";

                return new JsonResult(response);
            }

            response.IsSuccess = true;

            return new JsonResult(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetGeneratedContentAsync(long timestamp)
        {
            //https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/GettingStarted.html
            var item = await GetItemFromDynamo(timestamp);
																		  

            //https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_GetItem.html#API_GetItem_ResponseSyntax
            if (!item.TryGetValue("GeneratedContent", out var gc))
                throw new ArgumentException("GeneratedContent not yet available in DynamoDB.");

            string title = item.TryGetValue("ArticleTitle", out var titleValue) ? titleValue.S : "Default Title";
            string rekog_link = item.TryGetValue("RekognitionResponse", out var rekognitionLinkValue) ? rekognitionLinkValue.S : "Default Link";

            JObject rekognitionJson = JObject.Parse(rekog_link);
            string jsonAsString = rekognitionJson.ToString();
																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																									   
            
            var result = new
            {
                title,
                article = gc.S,
                rekognition_link = jsonAsString
            };

            return new JsonResult(result);
        }

        private async Task<bool> UploadImageToS3(IFormFile file, string filename)
        {
            // 1. Call S3 to Upload Image
            using var transfer = new TransferUtility(_s3);
            await using var stream = file.OpenReadStream();

            try
            {
                await transfer.UploadAsync(stream, GetInputBucketName(),filename);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return false;
            }
        }

        private async Task<bool> InsertItemToDynamo(string timestamp, string imageFilename)
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["Timestamp"] = new AttributeValue() { N = timestamp },
                ["ImageFilename"] = new AttributeValue() { S = imageFilename },
            };

            var request = new PutItemRequest
            {
                TableName = TableName,
                Item = item,
            };

            var response = await _dynamo.PutItemAsync(request);

            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }

        private async Task<Dictionary<string, AttributeValue>> GetItemFromDynamo(long timestamp)
        {
            var key = new Dictionary<string, AttributeValue>
            {
                ["Timestamp"] = new AttributeValue() { N = timestamp.ToString() },
            };

            var request = new GetItemRequest
            {
                TableName = TableName,
                Key = key
            };

            var response = await _dynamo.GetItemAsync(request);

            return response.Item;
        }

        private async Task<bool> IsImageValidAsync(IFormFile file)
        {
            await using var stream = file.OpenReadStream();

            try
            {
                var imageInfo = await Image.IdentifyAsync(stream);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string GetInputBucketName()
        {
            // Get bucket name from appsettings.json
            var bucketName = config.GetValue<string>("AwsContext:S3BucketInputName");

            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException(nameof(bucketName), "AwsContext:S3BucketInputName is null or invalid.");

            return bucketName;
        }

        //private string GetOutputBucketName()
        //{
        //    // Get bucket name from appsettings.json
        //    var bucketName = config.GetValue<string>("AwsContext:S3BucketOutputName");

        //    if (string.IsNullOrEmpty(bucketName))
        //        throw new ArgumentNullException(nameof(bucketName), "AwsContext:S3BucketOutputName is null or invalid.");

        //    return bucketName;
        //}

        private string GetFilenameWithTimestamp(string filename, string timestamp)
        {
            // Make filename unique
            var baseName = Path.GetFileNameWithoutExtension(filename);
            var ext = Path.GetExtension(filename);

            return $"{baseName}.{timestamp}{ext}";
        }

        //private string GetExpectedOutputFilename(string filenameWithTimestamp)
        //{
        //    // Return expected output filename
        //    var pos = filenameWithTimestamp.LastIndexOf(".");
        //    return filenameWithTimestamp.Substring(0, pos < 0 ? filenameWithTimestamp.Length : pos) + ".txt";
        //}
    }
}
