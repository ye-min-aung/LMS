using LMSPlatform.Models;

namespace LMSPlatform.Services;

public interface IVideoService
{
    // Video URL generation
    Task<string> GetSecureVideoUrlAsync(string videoKey, int userId);
    Task<string> GetVideoStreamUrlAsync(int lessonId, int userId);
    
    // Video upload and management
    Task<string> UploadVideoAsync(IFormFile videoFile, string courseId, string lessonTitle);
    Task<bool> DeleteVideoAsync(string videoKey);
    Task<VideoMetadata?> GetVideoMetadataAsync(string videoKey);
    
    // Access control
    Task<bool> ValidateVideoAccessAsync(int userId, int lessonId);
    Task<bool> CanUserAccessVideoAsync(int userId, string videoKey);
    
    // Video processing
    Task<bool> IsVideoProcessingCompleteAsync(string videoKey);
    Task<VideoProcessingStatus> GetVideoProcessingStatusAsync(string videoKey);
    
    // Streaming support
    Task<string> GenerateSignedUrlAsync(string videoKey, TimeSpan expiration);
    Task<VideoStreamInfo> GetVideoStreamInfoAsync(string videoKey);
}

public class VideoMetadata
{
    public string VideoKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

public class VideoStreamInfo
{
    public string StreamUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public List<VideoQuality> AvailableQualities { get; set; } = new();
    public bool IsLive { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class VideoQuality
{
    public string Label { get; set; } = string.Empty; // "720p", "1080p", etc.
    public string Url { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int Bitrate { get; set; }
}

public enum VideoProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    NotFound
}