using Microsoft.AspNetCore.Authorization;

namespace LMSPlatform.Services.Authorization;

/// <summary>
/// Requirement for accessing course content
/// </summary>
public class CourseAccessRequirement : IAuthorizationRequirement
{
    public bool RequireEnrollment { get; }
    public bool RequireApproval { get; }

    public CourseAccessRequirement(bool requireEnrollment = true, bool requireApproval = true)
    {
        RequireEnrollment = requireEnrollment;
        RequireApproval = requireApproval;
    }
}

/// <summary>
/// Requirement for accessing lesson content
/// </summary>
public class LessonAccessRequirement : IAuthorizationRequirement
{
    public bool CheckPrerequisites { get; }

    public LessonAccessRequirement(bool checkPrerequisites = true)
    {
        CheckPrerequisites = checkPrerequisites;
    }
}

/// <summary>
/// Requirement for admin-only operations
/// </summary>
public class AdminOperationRequirement : IAuthorizationRequirement
{
    public string Operation { get; }

    public AdminOperationRequirement(string operation)
    {
        Operation = operation;
    }
}
