using DeanOfficeCourseWork.Models;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<GroupSubject> GroupSubjects => Set<GroupSubject>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Attendance> Attendance => Set<Attendance>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();
    public DbSet<StatementRequest> StatementRequests => Set<StatementRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.UserName).IsUnique();
        modelBuilder.Entity<Student>().HasIndex(x => x.StudentBookNumber).IsUnique();
        modelBuilder.Entity<Subject>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<GroupSubject>()
            .HasIndex(x => new { x.GroupId, x.SubjectId, x.TeacherId, x.SemesterId })
            .IsUnique();

        modelBuilder.Entity<Student>().Property(x => x.BirthDate).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<Semester>().Property(x => x.StartDate).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<Semester>().Property(x => x.EndDate).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<Grade>().Property(x => x.GradeDate).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<Attendance>().Property(x => x.LessonDate).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<StatementRequest>().Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<StatementRequest>().Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<StatementRequest>().Property(x => x.PrintedAt).HasColumnType("timestamp without time zone");

        modelBuilder.Entity<User>()
            .HasOne(x => x.Student)
            .WithOne(x => x.User)
            .HasForeignKey<User>(x => x.StudentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>()
            .HasOne(x => x.Teacher)
            .WithOne(x => x.User)
            .HasForeignKey<User>(x => x.TeacherId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>()
            .HasOne(x => x.User)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<VerificationCode>()
            .HasOne(x => x.User)
            .WithMany(x => x.VerificationCodes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VerificationCode>().HasIndex(x => new { x.UserId, x.Purpose, x.Code });

        modelBuilder.Entity<StatementRequest>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StatementRequest>()
            .HasOne(x => x.Subject)
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StatementRequest>()
            .HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Teacher" },
            new Role { Id = 3, Name = "Student" });

        modelBuilder.Entity<Faculty>().HasData(
            new Faculty { Id = 1, Name = "Факультет информационных технологий", Code = "ФИТ" });

        modelBuilder.Entity<Specialty>().HasData(
            new Specialty { Id = 1, Name = "Информационные системы и технологии", Code = "ИСиТ", FacultyId = 1 });

        modelBuilder.Entity<Group>().HasData(
            new Group { Id = 1, Name = "ИС-21", Course = 2, YearOfAdmission = 2024, SpecialtyId = 1 });

        modelBuilder.Entity<Student>().HasData(
            new Student { Id = 1, LastName = "Иванов", FirstName = "Иван", MiddleName = "Иванович", StudentBookNumber = "2024-001", BirthDate = new DateTime(2005, 5, 12), GroupId = 1, IsActive = true },
            new Student { Id = 2, LastName = "Петрова", FirstName = "Анна", MiddleName = "Сергеевна", StudentBookNumber = "2024-002", BirthDate = new DateTime(2005, 9, 3), GroupId = 1, IsActive = true });

        modelBuilder.Entity<Teacher>().HasData(
            new Teacher { Id = 1, LastName = "Сидоров", FirstName = "Петр", MiddleName = "Алексеевич", Department = "Кафедра программирования", Email = "sidorov@example.edu" });

        modelBuilder.Entity<Subject>().HasData(
            new Subject { Id = 1, Name = "Базы данных", Code = "DB", Hours = 96 },
            new Subject { Id = 2, Name = "Объектно-ориентированное программирование", Code = "OOP", Hours = 120 });

        modelBuilder.Entity<Semester>().HasData(
            new Semester { Id = 1, Name = "Весенний семестр", Number = 4, AcademicYear = "2025/2026", StartDate = new DateTime(2026, 2, 1), EndDate = new DateTime(2026, 6, 30) });

        modelBuilder.Entity<GroupSubject>().HasData(
            new GroupSubject { Id = 1, GroupId = 1, SubjectId = 1, TeacherId = 1, SemesterId = 1 },
            new GroupSubject { Id = 2, GroupId = 1, SubjectId = 2, TeacherId = 1, SemesterId = 1 });

        modelBuilder.Entity<Grade>().HasData(
            new Grade { Id = 1, StudentId = 1, GroupSubjectId = 1, Value = 8, GradeType = "Экзамен", GradeDate = new DateTime(2026, 4, 10), Comment = "Хорошая работа" },
            new Grade { Id = 2, StudentId = 2, GroupSubjectId = 1, Value = 3, GradeType = "Экзамен", GradeDate = new DateTime(2026, 4, 10), Comment = "Нужна пересдача" });

        modelBuilder.Entity<Attendance>().HasData(
            new Attendance { Id = 1, StudentId = 1, GroupSubjectId = 1, LessonDate = new DateTime(2026, 4, 1), IsPresent = true },
            new Attendance { Id = 2, StudentId = 2, GroupSubjectId = 1, LessonDate = new DateTime(2026, 4, 1), IsPresent = false, Comment = "Отсутствовала" });

        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, UserName = "admin", PasswordHash = "admin123", FullName = "Администратор", RoleId = 1, Email = "admin@example.edu", PhoneNumber = "+375291111111", EmailConfirmed = true, PhoneNumberConfirmed = true },
            new User { Id = 2, UserName = "teacher", PasswordHash = "teacher123", FullName = "Сидоров Петр Алексеевич", RoleId = 2, TeacherId = 1, Email = "sidorov@example.edu", PhoneNumber = "+375292222222", EmailConfirmed = true, PhoneNumberConfirmed = false },
            new User { Id = 3, UserName = "student", PasswordHash = "student123", FullName = "Иванов Иван Иванович", RoleId = 3, StudentId = 1, Email = "student@example.edu", PhoneNumber = "+375293333333", EmailConfirmed = false, PhoneNumberConfirmed = false });
    }
}
