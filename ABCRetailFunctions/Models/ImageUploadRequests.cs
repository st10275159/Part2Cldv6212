namespace ABCRetailFunctions.Models
{
    public class ImageUploadRequest
    {
        public string Base64Image { get; set; } = string.Empty;
        public string? FileExtension { get; set; }
        public string? ContentType { get; set; }
        public string? Description { get; set; }
    }
}