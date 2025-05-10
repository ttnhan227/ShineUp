namespace Server.DTOs
{
    public class VideoDTO
    {
        public int VideoID { get; set; }
        public int UserID { get; set; }
        public int CategoryID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string VideoURL { get; set; }
        public string ThumbnailURL { get; set; }
        public int PrivacyID { get; set; }
        public DateTime UploadDate { get; set; }
    }
}