using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSPlatform.Models;
using LMSPlatform.Services;

namespace LMSPlatform.Pages.Assignment;

[Authorize]
public class SubmitModel : PageModel
{
    private readonly IAssignmentService _assignmentService;
    private readonly IUserService _userService;
    private readonly ILogger<SubmitModel> _logger;

    public SubmitModel(
        IAssignmentService assignmentService,
        IUserService userService,
        ILogger<SubmitModel> logger)
    {
        _assignmentService = assignmentService;
        _userService = userService;
        _logger = logger;
    }

    public Models.Assignment? Assignment { get; set; }
    public AssignmentSubmission? ExistingSubmission { get; set; }

    public async Task<IActionResult> OnGetAsync(int assignmentId)
    {
        // Get current user
        var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        // Get assignment details
        Assignment = await _assignmentService.GetAssignmentByIdAsync(assignmentId);
        if (Assignment == null)
        {
            return NotFound("Assignment not found");
        }

        // Check if user can submit this assignment
        if (!await _assignmentService.CanUserSubmitAssignmentAsync(user.Id, assignmentId))
        {
            TempData["ErrorMessage"] = "You cannot submit this assignment. You may not have access to the lesson or the assignment may be overdue.";
            
            if (Assignment.LessonID.HasValue)
            {
                return RedirectToPage("/Lessons/Watch", new { lessonId = Assignment.LessonID.Value });
            }
            else
            {
                return RedirectToPage("/Dashboard/Index");
            }
        }

        // Get existing submission if any
        ExistingSubmission = await _assignmentService.GetSubmissionAsync(user.Id, assignmentId);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int assignmentId, IFormFile assignmentFile)
    {
        // Get current user
        var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        // Get assignment details
        Assignment = await _assignmentService.GetAssignmentByIdAsync(assignmentId);
        if (Assignment == null)
        {
            return NotFound("Assignment not found");
        }

        // Validate file
        if (assignmentFile == null || assignmentFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to upload.";
            return await OnGetAsync(assignmentId);
        }

        // Check file type
        if (!await _assignmentService.IsFileTypeAllowedAsync(assignmentId, assignmentFile.FileName))
        {
            TempData["ErrorMessage"] = $"File type not allowed. Allowed types: {Assignment.AllowedFileTypes}";
            return await OnGetAsync(assignmentId);
        }

        // Check file size
        if (!await _assignmentService.IsFileSizeValidAsync(assignmentId, assignmentFile.Length))
        {
            var maxSizeMB = Assignment.MaxFileSize / 1024 / 1024;
            TempData["ErrorMessage"] = $"File size exceeds maximum allowed size of {maxSizeMB}MB.";
            return await OnGetAsync(assignmentId);
        }

        try
        {
            // Submit assignment
            var submission = await _assignmentService.SubmitAssignmentAsync(user.Id, assignmentId, assignmentFile);

            TempData["SuccessMessage"] = "Assignment submitted successfully! Your instructor will review it soon.";
            
            _logger.LogInformation("Assignment submitted: {SubmissionId} by User {UserId} for Assignment {AssignmentId}", 
                submission.SubmissionID, user.Id, assignmentId);

            return RedirectToPage("/Assignment/Submit", new { assignmentId = assignmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting assignment {AssignmentId} by User {UserId}", assignmentId, user.Id);
            TempData["ErrorMessage"] = "Error submitting assignment: " + ex.Message;
            return await OnGetAsync(assignmentId);
        }
    }
}