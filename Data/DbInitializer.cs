using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Models;

namespace LMSPlatform.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<LMSDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed roles if they don't exist
        await SeedRolesAsync(roleManager);

        // Seed admin user if it doesn't exist
        await SeedAdminUserAsync(userManager);

        // Sample data seeding disabled for now to avoid circular references
        // if (!context.Courses.Any())
        // {
        //     await SeedSampleDataAsync(context);
        // }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
    {
        string[] roles = { UserRoles.Admin, UserRoles.Student, UserRoles.Guest };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<User> userManager)
    {
        const string adminEmail = "admin@lms.com";
        const string adminPassword = "Admin123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                Role = UserRoles.Admin,
                IsApprovedStudent = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            }
        }
    }

    private static async Task SeedSampleDataAsync(LMSDbContext context)
    {
        // Create sample course
        var course = new Course
        {
            Title_EN = "Introduction to Web Development",
            Title_MM = "ဝဘ်ဖွံ့ဖြိုးမှုအခြေခံ",
            Description_EN = "Learn the basics of web development including HTML, CSS, and JavaScript.",
            Description_MM = "HTML၊ CSS နှင့် JavaScript အပါအဝင် ဝဘ်ဖွံ့ဖြိုးမှုအခြေခံများကို လေ့လာပါ။",
            Price = 99.99m
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync();

        // Create sample module
        var module = new Module
        {
            CourseID = course.CourseID,
            Title_EN = "HTML Fundamentals",
            Title_MM = "HTML အခြေခံများ",
            ModuleOrder = 1
        };

        context.Modules.Add(module);
        await context.SaveChangesAsync();

        // Create sample lessons
        var lessons = new[]
        {
            new Lesson
            {
                ModuleID = module.ModuleID,
                Title_EN = "Introduction to HTML",
                Title_MM = "HTML မိတ်ဆက်",
                LessonOrder = 1,
                LessonType = LessonTypes.Video,
                ContentURL = "https://example.com/video1.mp4"
            },
            new Lesson
            {
                ModuleID = module.ModuleID,
                Title_EN = "HTML Elements and Tags",
                Title_MM = "HTML Elements နှင့် Tags များ",
                LessonOrder = 2,
                LessonType = LessonTypes.Text,
                Content_EN = "HTML elements are the building blocks of web pages...",
                Content_MM = "HTML elements များသည် ဝဘ်စာမျက်နှာများ၏ အခြေခံအုတ်မြစ်များဖြစ်သည်..."
            }
        };

        context.Lessons.AddRange(lessons);
        await context.SaveChangesAsync();

        // Create sample quiz
        var quiz = new Quiz
        {
            LessonID = lessons[0].LessonID,
            Title = "HTML Basics Quiz",
            Instructions = "Test your knowledge of HTML basics",
            RequiredToUnlock = true,
            PassingScore = 70
        };

        context.Quizzes.Add(quiz);
        await context.SaveChangesAsync();

        // Create sample questions
        var question = new Question
        {
            QuizID = quiz.QuizID,
            QuestionText = "What does HTML stand for?",
            QuestionOrder = 1
        };

        context.Questions.Add(question);
        await context.SaveChangesAsync();

        // Create sample answer choices
        var choices = new[]
        {
            new AnswerChoice
            {
                QuestionID = question.QuestionID,
                ChoiceText = "HyperText Markup Language",
                IsCorrect = true,
                ChoiceOrder = 1
            },
            new AnswerChoice
            {
                QuestionID = question.QuestionID,
                ChoiceText = "High Tech Modern Language",
                IsCorrect = false,
                ChoiceOrder = 2
            },
            new AnswerChoice
            {
                QuestionID = question.QuestionID,
                ChoiceText = "Home Tool Markup Language",
                IsCorrect = false,
                ChoiceOrder = 3
            }
        };

        context.AnswerChoices.AddRange(choices);
        await context.SaveChangesAsync();
    }
}