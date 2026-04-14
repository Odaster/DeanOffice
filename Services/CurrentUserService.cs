using System.Security.Claims;

namespace DeanOfficeCourseWork.Services;

public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId => int.TryParse(User?.FindFirstValue("UserId"), out var id) ? id : null;
    public int? StudentId => int.TryParse(User?.FindFirstValue("StudentId"), out var id) ? id : null;
    public int? TeacherId => int.TryParse(User?.FindFirstValue("TeacherId"), out var id) ? id : null;
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
    public bool IsAdmin => Role == "Admin";
    public bool IsTeacher => Role == "Teacher";
    public bool IsStudent => Role == "Student";

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
}
