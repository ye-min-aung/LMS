# LMS Platform

A full-featured Learning Management System built with ASP.NET Core 8.0.

## Features

- **User Management**: Registration, authentication, role-based access (Admin/Student)
- **Course Management**: Create courses with modules and lessons
- **Content Types**: Video lessons (AWS S3) and text/HTML lessons
- **Assessments**: Quizzes with auto-grading and assignments with file submissions
- **Progress Tracking**: Lesson completion and course progress
- **Payments**: KBZ Pay integration for paid courses
- **Certificates**: Auto-generated certificates on course completion
- **Multi-language**: English and Myanmar language support

## Tech Stack

- ASP.NET Core 8.0 Razor Pages
- Entity Framework Core with MySQL
- ASP.NET Core Identity
- AWS S3 for video storage
- Bootstrap 5 UI

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- MySQL Server
- (Optional) AWS Account for S3 video hosting

### Setup

1. Clone the repository
```bash
git clone https://github.com/YOUR_USERNAME/LMSPlatform.git
cd LMSPlatform
```

2. Update `appsettings.json` with your MySQL connection string

3. Run the application
```bash
dotnet run
```

4. Access at `https://localhost:7000`

### Default Admin Login
- Email: `admin@lms.com`
- Password: `Admin123!`

## Configuration

Copy `appsettings.Production.json.template` to `appsettings.Production.json` and update:

- Database connection string
- KBZ Pay credentials (for payments)
- AWS S3 credentials (for video hosting)
- SMTP settings (for emails)

## License

MIT
