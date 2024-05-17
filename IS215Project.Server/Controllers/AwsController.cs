using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

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
            var timestamp = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            var filename = GetFilenameWithTimestamp(file.FileName, timestamp);

            // TODO retry if the timestamp (Partition key) is duplicate
            if (!await InsertItemToDynamo(timestamp, filename))
            {
                throw new Exception($"Failed to insert record to DynamoDb - {TableName} table.");
            }

            await UploadImageToS3(file, filename);

            return new JsonResult(timestamp);
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

        private async Task UploadImageToS3(IFormFile file, string filename)
        {
            // 1. Call S3 to Upload Image
            using var transfer = new TransferUtility(_s3);

            await using var stream = file.OpenReadStream();

            await transfer.UploadAsync(
                stream,
                GetInputBucketName(),
                filename
            );
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
