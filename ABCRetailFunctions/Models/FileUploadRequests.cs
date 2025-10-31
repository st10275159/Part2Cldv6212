namespace ABCRetailFunctions.Models
{
    public class FileUploadRequest
    {
        public string Base64Content { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? Description { get; set; }
    }
}
