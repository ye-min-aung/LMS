using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSPlatform.Models;
using LMSPlatform.Services;

namespace LMSPlatform.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AssignmentsModel : PageModel
{
    private readonly IAssignmentService _assignmentService;
    private readonly IUserService _userService;

    public AssignmentsModel(IAssignmentService assignmentService, IUserService userService)
    {
        _assignmentService = assignmentService;
        _userService = userService;
    }

    public List<AssignmentSubmission> Submissions { get; set; } = new();
    public string Filter { get; set; } = "pending";
    public int PendingCount { get; set; }

    public async Task OnGetAsync(string? filter)
    {
        Filter = filter ?? "pending";
        
        var allSubmissions = await _assignmentService.GetPendingSubmissionsAsync();
        PendingCount = allSubmissions.Count();

        Submissions = Filter switch
        {
            "pending" => allSubmissions.ToList(),
            "reviewed" => (await _assignmentService.GetSubmissionsForReviewAsync(0))
                .Where(s => s.Status != SubmissionStatus.Pending).ToList(),
            _ => (await _assignmentService.GetSubmissionsForReviewAsync(0)).ToList()
        };
    }

    public async Task<IActionResult> OnPostReviewAsync(int submissionId, string status, decimal? grade, string? feedback)
    {
        var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
        if (user == null) return RedirectToPage();

        await _assignmentService.ReviewSubmissionAsync(submissionId, status, feedback, user.Id, grade);
        
        return RedirectToPage();
    }
}
