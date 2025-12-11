using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Services;

public class VideoService : IVideoService
{
    private readonly IAmazonS3 _s3Client;
    private readonly LMSDbContext _context;
    private readonly IPrerequisiteService _prerequisiteService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VideoService> _logger;
    private readonly string _bucketName;

    public VideoService(
        IAmazonS3 s3Client,
        LMSDbContext context,
        IPrerequisiteService prerequisiteService,
        IConfiguration configuration,
        ILogger<VideoService> logger)
    {
        _s3Client = s3Client;
        _context = context;
        _prerequisiteService = prerequisiteService;
        _configuration = configuration;
        _logger = logger;
        _bucketName = _configuration["AWS:BucketName"] ?? "lms-video-content";
    }

    #region Video URL Generation

    public async Task<string> GetSecureVideoUrlAsync(string videoKey, int userId)
    {
        // Validate access first
        if (!await CanUserAccessVideoAsync(userId, videoKey))
        {
            throw new UnauthorizedAccessException("User does not have access to this video");
        }

        // Generate signed URL with 4-hour expiration
        var signedUrl = await GenerateSignedUrlAsync(videoKey, TimeSpan.FromHours(4));
        
        _logger.LogInformation("Generated secure video URL for user {UserId}, video {VideoKey}", userId, videoKey);
        return signedUrl;
    }

    public async Task<string> GetVideoStreamUrlAsync(int lessonId, int userId)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson == null || lesson.LessonType != LessonTypes.Video)
        {
            throw new ArgumentException("Lesson not found or is not a video lesson");
        }

        // Validate lesson access
        if (!await _prerequisiteService.CanAccessLessonAsync(userId, lessonId))
        {
            throw new UnauthorizedAccessException("User does not have access to this lesson");
        }

        if (string.IsNullOrEmpty(lesson.ContentURL))
        {
            throw new InvalidOperationException("Video content URL not found for lesson");
        }

        // Extract video key from content URL
        var videoKey = ExtractVideoKeyFromUrl(lesson.ContentURL);
        return await GetSecureVideoUrlAsync(videoKey, userId);
    }

    #endregion

    #region Video Upload and Management

    public async Task<string> UploadVideoAsync(IFormFile videoFile, string courseId, string lessonTitle)
    {
        if (videoFile == null || videoFile.Length == 0)
        {
            throw new ArgumentException("Video file is required");
        }

        // Validate file type
        var allowedTypes = new[] { "video/mp4", "video/avi", "video/mov", "video/wmv" };
        if (!allowedTypes.Contains(videoFile.ContentType.ToLower()))
        {
            throw new ArgumentException("Invalid video file type. Supported formats: MP4, AVI, MOV, WMV");
        }

        // Generate unique video key
        var videoKey = GenerateVideoKey(courseId, lessonTitle, videoFile.FileName);

        try
        {
            using var stream = videoFile.OpenReadStream();
            
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = videoKey,
                InputStream = stream,
                ContentType = videoFile.ContentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                Metadata =
                {
                    ["original-filename"] = videoFile.FileName,
                    ["course-id"] = courseId,
                    ["lesson-title"] = lessonTitle,
                    ["uploaded-at"] = DateTime.UtcNow.ToString("O")
                }
            };

            var response = await _s3Client.PutObjectAsync(request);
            
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Video uploaded successfully: {VideoKey}", videoKey);
                return $"https://{_bucketName}.s3.amazonaws.com/{videoKey}";
            }
            else
            {
                throw new InvalidOperationException($"Failed to upload video. Status: {response.HttpStatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading video {VideoKey}", videoKey);
            throw;
        }
    }

    public async Task<bool> DeleteVideoAsync(string videoKey)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = videoKey
            };

            var response = await _s3Client.DeleteObjectAsync(request);
            
            _logger.LogInformation("Video deleted: {VideoKey}", videoKey);
            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting video {VideoKey}", videoKey);
            return false;
        }
    }

    public async Task<VideoMetadata?> GetVideoMetadataAsync(string videoKey)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = videoKey
            };

            var response = await _s3Client.GetObjectMetadataAsync(request);

            return new VideoMetadata
            {
                VideoKey = videoKey,
                OriginalFileName = response.Metadata.Keys.Contains("original-filename") 
                    ? response.Metadata["original-filename"] : "Unknown",
                FileSizeBytes = response.ContentLength,
                ContentType = response.Headers.ContentType,
                UploadedAt = response.LastModified
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video metadata for {VideoKey}", videoKey);
            return null;
        }
    }

    #endregion

    #region Access Control

    public async Task<bool> ValidateVideoAccessAsync(int userId, int lessonId)
    {
        return await _prerequisiteService.CanAccessLessonAsync(userId, lessonId);
    }

    public async Task<bool> CanUserAccessVideoAsync(int userId, string videoKey)
    {
        // Find lesson with this video key
        var lesson = await _context.Lessons
            .Include(l => l.Module)
            .FirstOrDefaultAsync(l => l.ContentURL != null && l.ContentURL.Contains(videoKey));

        if (lesson == null)
        {
            return false;
        }

        return await _prerequisiteService.CanAccessLessonAsync(userId, lesson.LessonID);
    }

    #endregion

    #region Video Processing

    public async Task<bool> IsVideoProcessingCompleteAsync(string videoKey)
    {
        // For basic implementation, assume all uploaded videos are ready
        // In production, you might integrate with AWS MediaConvert or similar service
        var metadata = await GetVideoMetadataAsync(videoKey);
        return metadata != null;
    }

    public async Task<VideoProcessingStatus> GetVideoProcessingStatusAsync(string videoKey)
    {
        var metadata = await GetVideoMetadataAsync(videoKey);
        
        if (metadata == null)
        {
            return VideoProcessingStatus.NotFound;
        }

        // Simple implementation - in production you'd check actual processing status
        return VideoProcessingStatus.Completed;
    }

    #endregion

    #region Streaming Support

    public async Task<string> GenerateSignedUrlAsync(string videoKey, TimeSpan expiration)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = videoKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(expiration)
            };

            var signedUrl = await _s3Client.GetPreSignedURLAsync(request);
            return signedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signed URL for {VideoKey}", videoKey);
            throw;
        }
    }

    public async Task<VideoStreamInfo> GetVideoStreamInfoAsync(string videoKey)
    {
        var metadata = await GetVideoMetadataAsync(videoKey);
        if (metadata == null)
        {
            throw new FileNotFoundException($"Video not found: {videoKey}");
        }

        var streamUrl = await GenerateSignedUrlAsync(videoKey, TimeSpan.FromHours(4));
        var thumbnailUrl = await GenerateThumbnailUrlAsync(videoKey);

        return new VideoStreamInfo
        {
            StreamUrl = streamUrl,
            ThumbnailUrl = thumbnailUrl,
            Duration = metadata.Duration,
            AvailableQualities = new List<VideoQuality>
            {
                new VideoQuality { Label = "Original", Url = streamUrl, Width = 0, Height = 0, Bitrate = 0 }
            },
            IsLive = false,
            ExpiresAt = DateTime.UtcNow.AddHours(4)
        };
    }

    #endregion

    #region Private Helper Methods

    private string GenerateVideoKey(string courseId, string lessonTitle, string originalFileName)
    {
        var sanitizedTitle = SanitizeFileName(lessonTitle);
        var extension = Path.GetExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        
        return $"courses/{courseId}/videos/{sanitizedTitle}-{timestamp}-{uniqueId}{extension}";
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return sanitized.Replace(" ", "-").ToLower();
    }

    private string ExtractVideoKeyFromUrl(string contentUrl)
    {
        // Extract key from S3 URL format: https://bucket.s3.amazonaws.com/key
        var uri = new Uri(contentUrl);
        return uri.AbsolutePath.TrimStart('/');
    }

    private async Task<string> GenerateThumbnailUrlAsync(string videoKey)
    {
        // Generate thumbnail key (assuming thumbnails are stored with .jpg extension)
        var thumbnailKey = Path.ChangeExtension(videoKey, ".jpg");
        
        try
        {
            // Check if thumbnail exists
            var metadata = await GetVideoMetadataAsync(thumbnailKey);
            if (metadata != null)
            {
                return await GenerateSignedUrlAsync(thumbnailKey, TimeSpan.FromHours(4));
            }
        }
        catch
        {
            // Thumbnail doesn't exist, return placeholder
        }

        // Return placeholder thumbnail URL
        return "/images/video-placeholder.jpg";
    }

    #endregion
}