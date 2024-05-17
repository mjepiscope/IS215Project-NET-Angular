namespace IS215Project.Server.Models
{
    public class UploadImageResponse
    {
        public bool IsSuccess { get; set; }
        public string Timestamp { get; set; }
        public string ImageFilename { get; set; }
        public string ErrorMessage { get; set; }
    }
}
