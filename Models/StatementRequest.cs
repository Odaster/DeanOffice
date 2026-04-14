using System.ComponentModel.DataAnnotations;

namespace DeanOfficeCourseWork.Models;

public class StatementRequest
{
    public const string TypeEducationCertificate = "Справка об обучении";
    public const string TypeRetake = "Пересдача";

    public const string StatusPending = "На рассмотрении";
    public const string StatusRejected = "Отклонена";
    public const string StatusPrinted = "Напечатана";

    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    [Required, MaxLength(40)]
    public string RequestType { get; set; } = TypeEducationCertificate;

    [Required, MaxLength(40)]
    public string Status { get; set; } = StatusPending;

    public bool CertificateWithEmblem { get; set; }

    [MaxLength(80)]
    public string? CertificatePurpose { get; set; }

    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    [DataType(DataType.Date)]
    public DateTime? MissedDate { get; set; }

    [MaxLength(40)]
    public string? AbsenceReason { get; set; }

    [MaxLength(500)]
    public string? AdminComment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? PrintedAt { get; set; }
}
