using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSPlatform.Models;
using LMSPlatform.Services;

namespace LMSPlatform.Pages.Lessons;

[Authorize]
public class WatchModel : PageModel
{
    private readonly ICourseService _courseService;
    private readonly IVideoService _videoService;
    private readonly ILessonProgressService _progressService;
    private readonly IPrerequisiteService _prerequisiteService;
    private readonly IUserService _userService;

    public WatchModel(
        ICourseService courseService,
        IVideoService videoService,
        ILessonProgressService progressService,
        IPrerequisiteService prerequisiteService,
        IUserService userService)
    {
        _courseService = courseService;
        _videoService = videoService;
        _progressService = progressService;
        _prerequisiteService = prerequisiteService;
        _userService = userService;
    }

    public Lesson? Lesson { get; set; }
    public string? VideoStreamUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public LessonProgress? LessonProgress { get; set; }
    public Lesson? NextLesson { get; set; }
    public Lesson? PreviousLesson { get; set; }
    public int EnrollmentId { get; set; }
    public bool IsLessonCompleted { get; set; }
    public decimal CourseProgressPercentage { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public List<Lesson> ModuleLessons { get; set; } = new();
    public List<LessonProgress> LessonProgresses { get; set; } = new();
    public QuizAttempt? QuizAttempt { get; set; }
    public AssignmentSubmission? AssignmentSubmission { get; set; }
    
    public string AssignmentGradeDisplay => AssignmentSubmission?.Grade.HasValue == true 
        ? $"{AssignmentSubmission.Grade.Value:F0}%" 
        : string.Empty;

    public async Task<IActionResult> OnGetAsync(int lessonId)
    {
        // Get current user
        var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        // Get lesson details
        Lesson = await _courseService.GetLessonByIdAsync(lessonId);
        if (Lesson == null)
        {
            return NotFound("Lesson not found");
        }

        // Check if user can access this lesson
        if (!await _prerequisiteService.CanAccessLessonAsync(user.Id, lessonId))
        {
            TempData["ErrorMessage"] = "You don't have access to this lesson. Please complete the previous lessons first.";
            return RedirectToPage("/Courses/Details", new { id = Lesson.Module.CourseID });
        }

        // Get enrollment
        var enrollment = await _courseService.GetEnrollmentAsync(user.Id, Lesson.Module.CourseID);
        if (enrollment == null)
        {
            return RedirectToPage("/Courses/Details", new { id = Lesson.Module.CourseID });
        }

        EnrollmentId = enrollment.EnrollmentID;

        // Get lesson progress
        LessonProgress = await _progressService.GetLessonProgressAsync(EnrollmentId, lessonId);
        IsLessonCompleted = LessonProgress?.IsCompleted ?? false;

        // Get video stream URL if it's a video lesson
        if (Lesson.LessonType == LessonTypes.Video && !string.IsNullOrEmpty(Lesson.ContentURL))
        {
            try
            {
                VideoStreamUrl = await _videoService.GetVideoStreamUrlAsync(lessonId, user.Id);
                var videoStreamInfo = await _videoService.GetVideoStreamInfoAsync(ExtractVideoKey(Lesson.ContentURL));
                ThumbnailUrl = videoStreamInfo.ThumbnailUrl;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to load video content: " + ex.Message;
            }
        }

        // Get navigation lessons
        NextLesson = await _courseService.GetNextLessonAsync(lessonId, user.Id);
        PreviousLesson = await _courseService.GetPreviousLessonAsync(lessonId, user.Id);

        // Get course progress
        var progressSummary = await _progressService.GetCourseProgressSummaryAsync(EnrollmentId);
        CourseProgressPercentage = progressSummary.ProgressPercentage;
        CompletedLessons = progressSummary.CompletedLessons;
        TotalLessons = progressSummary.TotalLessons;

        // Get module lessons
        ModuleLessons = (await _courseService.GetModuleLessonsAsync(Lesson.ModuleID)).ToList();
        LessonProgresses = (await _progressService.GetEnrollmentProgressAsync(EnrollmentId))
            .Where(lp => ModuleLessons.Any(ml => ml.LessonID == lp.LessonID))
            .ToList();

        // Get quiz attempt if lesson has quiz
        if (Lesson.Quiz != null)
        {
            // This would need to be implemented in a quiz service
            // QuizAttempt = await _quizService.GetLatestAttemptAsync(user.Id, Lesson.Quiz.QuizID);
        }

        // Get assignment submission if lesson has assignment
        if (Lesson.Assignment != null)
        {
            // This would need to be implemented in an assignment service
            // AssignmentSubmission = await _assignmentService.GetSubmissionAsync(user.Id, Lesson.Assignment.AssignmentID);
        }

        // Update lesson progress (mark as accessed)
        if (LessonProgress == null)
        {
            await _progressService.UpdateLessonProgressAsync(EnrollmentId, lessonId, false);
        }
        else
        {
            await _progressService.UpdateLessonProgressAsync(EnrollmentId, lessonId, LessonProgress.IsCompleted);
        }

        return Page();
    }

    private string ExtractVideoKey(string contentUrl)
    {
        // Extract key from S3 URL format
        var uri = new Uri(contentUrl);
        return uri.AbsolutePath.TrimStart('/');
    }
}