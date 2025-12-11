using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Models;

namespace LMSPlatform.Data;

public class LMSDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public LMSDbContext(DbContextOptions<LMSDbContext> options) : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<Course> Courses { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<LessonProgress> LessonProgresses { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<AnswerChoice> AnswerChoices { get; set; }
    public DbSet<QuizAttempt> QuizAttempts { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
    public DbSet<Certificate> Certificates { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure User entity
        builder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Role).HasDefaultValue("Student");
            entity.Property(e => e.IsApprovedStudent).HasDefaultValue(false);
        });

        // Configure Course entity
        builder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseID);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.HasIndex(e => e.Title_EN);
        });

        // Configure Module entity
        builder.Entity<Module>(entity =>
        {
            entity.HasKey(e => e.ModuleID);
            entity.HasOne(e => e.Course)
                  .WithMany(e => e.Modules)
                  .HasForeignKey(e => e.CourseID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CourseID, e.ModuleOrder }).IsUnique();
        });

        // Configure Lesson entity
        builder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.LessonID);
            entity.HasOne(e => e.Module)
                  .WithMany(e => e.Lessons)
                  .HasForeignKey(e => e.ModuleID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ModuleID, e.LessonOrder }).IsUnique();
        });

        // Configure Enrollment entity
        builder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentID);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Enrollments)
                  .HasForeignKey(e => e.UserID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Course)
                  .WithMany(e => e.Enrollments)
                  .HasForeignKey(e => e.CourseID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserID, e.CourseID }).IsUnique();
            entity.Property(e => e.Status).HasDefaultValue("Pending Payment");
        });

        // Configure Payment entity
        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentID);
            entity.HasOne(e => e.Enrollment)
                  .WithMany(e => e.Payments)
                  .HasForeignKey(e => e.EnrollmentID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.AmountPaid).HasPrecision(10, 2);
            entity.Property(e => e.AdminApprovalStatus).HasDefaultValue("Pending");
        });

        // Configure LessonProgress entity
        builder.Entity<LessonProgress>(entity =>
        {
            entity.HasKey(e => e.ProgressID);
            entity.HasOne(e => e.Enrollment)
                  .WithMany(e => e.LessonProgresses)
                  .HasForeignKey(e => e.EnrollmentID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Lesson)
                  .WithMany(e => e.LessonProgresses)
                  .HasForeignKey(e => e.LessonID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.EnrollmentID, e.LessonID }).IsUnique();
            entity.Property(e => e.IsCompleted).HasDefaultValue(false);
        });

        // Configure Quiz entity
        builder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.QuizID);
            entity.HasOne(e => e.Lesson)
                  .WithOne(e => e.Quiz)
                  .HasForeignKey<Quiz>(e => e.LessonID)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.RequiredToUnlock).HasDefaultValue(false);
            entity.Property(e => e.PassingScore).HasDefaultValue(70);
            entity.Property(e => e.MaxAttempts).HasDefaultValue(3);
        });

        // Configure Question entity
        builder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionID);
            entity.HasOne(e => e.Quiz)
                  .WithMany(e => e.Questions)
                  .HasForeignKey(e => e.QuizID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.QuizID, e.QuestionOrder }).IsUnique();
        });

        // Configure AnswerChoice entity
        builder.Entity<AnswerChoice>(entity =>
        {
            entity.HasKey(e => e.ChoiceID);
            entity.HasOne(e => e.Question)
                  .WithMany(e => e.AnswerChoices)
                  .HasForeignKey(e => e.QuestionID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.IsCorrect).HasDefaultValue(false);
        });

        // Configure QuizAttempt entity
        builder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptID);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.QuizAttempts)
                  .HasForeignKey(e => e.UserID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Quiz)
                  .WithMany(e => e.QuizAttempts)
                  .HasForeignKey(e => e.QuizID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Score).HasPrecision(5, 2);
            entity.Property(e => e.Passed).HasDefaultValue(false);
        });

        // Configure Assignment entity
        builder.Entity<Assignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentID);
            entity.HasOne(e => e.Lesson)
                  .WithOne(e => e.Assignment)
                  .HasForeignKey<Assignment>(e => e.LessonID)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.MaxFileSize).HasDefaultValue(10485760);
            entity.Property(e => e.AllowedFileTypes).HasDefaultValue("pdf,doc,docx,txt,zip");
        });

        // Configure AssignmentSubmission entity
        builder.Entity<AssignmentSubmission>(entity =>
        {
            entity.HasKey(e => e.SubmissionID);
            entity.HasOne(e => e.Assignment)
                  .WithMany(e => e.AssignmentSubmissions)
                  .HasForeignKey(e => e.AssignmentID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.AssignmentSubmissions)
                  .HasForeignKey(e => e.UserID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ReviewedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ReviewedBy)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.HasIndex(e => new { e.AssignmentID, e.UserID }).IsUnique();
        });

        // Configure Certificate entity
        builder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.CertificateID);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Certificates)
                  .HasForeignKey(e => e.UserID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Course)
                  .WithMany(e => e.Certificates)
                  .HasForeignKey(e => e.CourseID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UniqueCertID).IsUnique();
            entity.HasIndex(e => new { e.UserID, e.CourseID }).IsUnique();
        });

        // Seed default roles
        SeedRoles(builder);
    }

    private static void SeedRoles(ModelBuilder builder)
    {
        builder.Entity<IdentityRole<int>>().HasData(
            new IdentityRole<int>
            {
                Id = 1,
                Name = "Admin",
                NormalizedName = "ADMIN"
            },
            new IdentityRole<int>
            {
                Id = 2,
                Name = "Student",
                NormalizedName = "STUDENT"
            },
            new IdentityRole<int>
            {
                Id = 3,
                Name = "Guest",
                NormalizedName = "GUEST"
            }
        );
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified && e.Entity.GetType().GetProperty("UpdatedAt") != null);

        foreach (var entry in entries)
        {
            entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        }
    }
}