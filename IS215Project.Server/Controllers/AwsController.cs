using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;

namespace IS215Project.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AwsController : ControllerBase
    {
        private IAmazonS3 _client;
        //private readonly IAmazonS3 _client = new AmazonS3Client();

        [HttpGet]
        public bool TestConnection() => true;

        [HttpGet]
        public async Task<List<S3Bucket>> GetBuckets()
        {
            _client = new AmazonS3Client();
            
            var response = await _client.ListBucketsAsync();

            return response.Buckets;
        }

        //[HttpGet]
        //public Task<ListBucketsResponse> GetList()
        //    => _client.ListBucketsAsync();

        [HttpPost]
        public void GenerateContentFromImage()
        {
            UploadImageToS3();

            GetResponseFromLambda();
        }


        private void UploadImageToS3()
        {
            // TODO
            // 1. Call S3 to Upload Image
        }

        private void GetResponseFromLambda()
        {
            // TODO
            // 2. Return Lambda Response
        }
    }
}
