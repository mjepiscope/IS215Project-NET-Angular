using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using IS215Project.Server.Models;
using Microsoft.AspNetCore.Mvc;
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
            //var rekognition_link = await GetS3ObjectUrlAsync(timestamp);

            //https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_GetItem.html#API_GetItem_ResponseSyntax
            if (!item.TryGetValue("GeneratedContent", out var gc))
                throw new ArgumentException("GeneratedContent not yet available in DynamoDB.");

            string title = item.TryGetValue("ArticleTitle", out var titleValue) ? titleValue.S : "Default Title";
            //string rekog_link = item.TryGetValue("RekognitionResponse", out var rekognitionLinkValue) ? rekognitionLinkValue.S : "Default Link";

            //JObject rekognitionJson = JObject.Parse(rekog_link);
            //string jsonAsString = rekognitionJson.ToString();
            string rekog_link = "{\"FaceDetails\": [{\"BoundingBox\": {\"Width\": 0.15670375525951385, \"Height\": 0.13892687857151031, \"Left\": 0.523135781288147, \"Top\": 0.37672650814056396}, \"AgeRange\": {\"Low\": 21, \"High\": 29}, \"Smile\": {\"Value\": true, \"Confidence\": 99.87416076660156}, \"Eyeglasses\": {\"Value\": false, \"Confidence\": 99.85591888427734}, \"Sunglasses\": {\"Value\": false, \"Confidence\": 99.97154235839844}, \"Gender\": {\"Value\": \"Female\", \"Confidence\": 99.99735260009766}, \"Beard\": {\"Value\": false, \"Confidence\": 99.99934387207031}, \"Mustache\": {\"Value\": false, \"Confidence\": 100.0}, \"EyesOpen\": {\"Value\": true, \"Confidence\": 98.29013061523438}, \"MouthOpen\": {\"Value\": true, \"Confidence\": 99.61315155029297}, \"Emotions\": [{\"Type\": \"HAPPY\", \"Confidence\": 100.0}, {\"Type\": \"SURPRISED\", \"Confidence\": 0.0054836273193359375}, {\"Type\": \"CALM\", \"Confidence\": 0.0010311603546142578}, {\"Type\": \"CONFUSED\", \"Confidence\": 0.0004947185516357422}, {\"Type\": \"ANGRY\", \"Confidence\": 0.00016689300537109375}, {\"Type\": \"DISGUSTED\", \"Confidence\": 7.748603820800781e-05}, {\"Type\": \"FEAR\", \"Confidence\": 0.0}, {\"Type\": \"SAD\", \"Confidence\": 0.0}], \"Landmarks\": [{\"Type\": \"eyeLeft\", \"X\": 0.5555276870727539, \"Y\": 0.4345932602882385}, {\"Type\": \"eyeRight\", \"X\": 0.6247130632400513, \"Y\": 0.4276684820652008}, {\"Type\": \"mouthLeft\", \"X\": 0.5726903080940247, \"Y\": 0.48148003220558167}, {\"Type\": \"mouthRight\", \"X\": 0.6305722594261169, \"Y\": 0.4758027195930481}, {\"Type\": \"nose\", \"X\": 0.588902473449707, \"Y\": 0.45825955271720886}, {\"Type\": \"leftEyeBrowLeft\", \"X\": 0.5297629833221436, \"Y\": 0.4254939556121826}, {\"Type\": \"leftEyeBrowRight\", \"X\": 0.5640425682067871, \"Y\": 0.419526606798172}, {\"Type\": \"leftEyeBrowUp\", \"X\": 0.5447896122932434, \"Y\": 0.41873952746391296}, {\"Type\": \"rightEyeBrowLeft\", \"X\": 0.6034872531890869, \"Y\": 0.4154503643512726}, {\"Type\": \"rightEyeBrowRight\", \"X\": 0.6496514081954956, \"Y\": 0.4133133292198181}, {\"Type\": \"rightEyeBrowUp\", \"X\": 0.6242245435714722, \"Y\": 0.4105772376060486}, {\"Type\": \"leftEyeLeft\", \"X\": 0.5441060662269592, \"Y\": 0.43536078929901123}, {\"Type\": \"leftEyeRight\", \"X\": 0.5693237781524658, \"Y\": 0.4336438775062561}, {\"Type\": \"leftEyeUp\", \"X\": 0.5543658137321472, \"Y\": 0.4322853088378906}, {\"Type\": \"leftEyeDown\", \"X\": 0.5562945008277893, \"Y\": 0.4366454482078552}, {\"Type\": \"rightEyeLeft\", \"X\": 0.6110531091690063, \"Y\": 0.42943650484085083}, {\"Type\": \"rightEyeRight\", \"X\": 0.6377475261688232, \"Y\": 0.4259687066078186}, {\"Type\": \"rightEyeUp\", \"X\": 0.6237605810165405, \"Y\": 0.4253004789352417}, {\"Type\": \"rightEyeDown\", \"X\": 0.624769389629364, \"Y\": 0.4297890067100525}, {\"Type\": \"noseLeft\", \"X\": 0.5816647410392761, \"Y\": 0.4636584520339966}, {\"Type\": \"noseRight\", \"X\": 0.6071771383285522, \"Y\": 0.4610922038555145}, {\"Type\": \"mouthUp\", \"X\": 0.5965456962585449, \"Y\": 0.4738067090511322}, {\"Type\": \"mouthDown\", \"X\": 0.6010622978210449, \"Y\": 0.48789864778518677}, {\"Type\": \"leftPupil\", \"X\": 0.5555276870727539, \"Y\": 0.4345932602882385}, {\"Type\": \"rightPupil\", \"X\": 0.6247130632400513, \"Y\": 0.4276684820652008}, {\"Type\": \"upperJawlineLeft\", \"X\": 0.5262527465820312, \"Y\": 0.4367060959339142}, {\"Type\": \"midJawlineLeft\", \"X\": 0.5516440272331238, \"Y\": 0.48700153827667236}, {\"Type\": \"chinBottom\", \"X\": 0.6103324890136719, \"Y\": 0.5117583274841309}, {\"Type\": \"midJawlineRight\", \"X\": 0.6734098196029663, \"Y\": 0.4746311902999878}, {\"Type\": \"upperJawlineRight\", \"X\": 0.6764021515846252, \"Y\": 0.4213387966156006}], \"Pose\": {\"Roll\": -7.948850154876709, \"Yaw\": -4.922952651977539, \"Pitch\": 1.5444234609603882}, \"Quality\": {\"Brightness\": 69.59022521972656, \"Sharpness\": 73.32209777832031}, \"Confidence\": 99.99925231933594, \"FaceOccluded\": {\"Value\": false, \"Confidence\": 99.93297576904297}, \"EyeDirection\": {\"Yaw\": -0.8024238348007202, \"Pitch\": -4.845669269561768, \"Confidence\": 99.98931121826172}}], \"ResponseMetadata\": {\"RequestId\": \"0bb70bee-7357-4906-ae0b-c33fb9e2126e\", \"HTTPStatusCode\": 200, \"HTTPHeaders\": {\"x-amzn-requestid\": \"0bb70bee-7357-4906-ae0b-c33fb9e2126e\", \"content-type\": \"application/x-amz-json-1.1\", \"content-length\": \"3461\", \"date\": \"Thu, 16 May 2024 09:36:36 GMT\"}, \"RetryAttempts\": 0}}";
            
            var result = new
            {
                title,
                article = gc.S,
                rekognition_link = rekog_link
            };

            return new JsonResult(result);
        }

        //[HttpGet("getS3ObjectUrl")]
        //public Task<IActionResult> GetS3ObjectUrlAsync(long timestamp)
        //{
        //    try
        //    {
        //        var request = new GetPreSignedUrlRequest
        //        {
        //            BucketName = GetInputBucketName(),
        //            Key = timestamp + "_rekognition.txt",
        //            Expires = DateTime.UtcNow.AddHours(1), // Set expiration time as needed
        //            Verb = HttpVerb.GET,
        //            ResponseHeaderOverrides =
        //            {
        //                ContentType = "text/plain" 
        //            }
        //        };

        //        var url = _s3.GetPreSignedURL(request);

        //        return Task.FromResult<IActionResult>(Ok(url));
        //    }
        //    catch (Exception ex)
        //    {
                // Log the exception
        //        return Task.FromResult<IActionResult>(StatusCode(500, "Internal server error while generating S3 object URL."));
        //    }
        //}

        // Refresh the output bucket name
        //private async Task RefreshBucketAsync(string bucketName)
        //{
        //    try
        //    {
        //        var request = new ListObjectsV2Request
        //        {
        //            BucketName = bucketName,
        //        };

        //        ListObjectsV2Response response;
        //        do
        //        {
        //            response = await _s3.ListObjectsV2Async(request);
        //            foreach (S3Object entry in response.S3Objects)
        //            {
        //                System.Console.WriteLine($"Object key: {entry.Key}");
        //            }
        //            request.ContinuationToken = response.NextContinuationToken;
        //        } while (response.IsTruncated);
        //    }
        //    catch (AmazonS3Exception e)
        //    {
        //        System.Console.WriteLine("Error encountered on server. Message:'{0}' when listing objects", e.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        System.Console.WriteLine("Unknown encountered on server. Message:'{0}' when listing objects", e.Message);
        //    }
        //}

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
