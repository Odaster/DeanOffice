using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.Models;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Services;

public class VerificationCodeService
{
    public const string EmailConfirmation = "EmailConfirmation";
    public const string PhoneConfirmation = "PhoneConfirmation";
    public const string PasswordReset = "PasswordReset";

    private readonly ApplicationDbContext _context;

    public VerificationCodeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VerificationCode> CreateAsync(int userId, string purpose, string destination)
    {
        var oldCodes = await _context.VerificationCodes
            .Where(x => x.UserId == userId && x.Purpose == purpose && x.UsedAt == null)
            .ToListAsync();

        foreach (var oldCode in oldCodes)
        {
            oldCode.UsedAt = DateTime.UtcNow;
        }

        var verificationCode = new VerificationCode
        {
            UserId = userId,
            Purpose = purpose,
            Destination = destination,
            Code = Random.Shared.Next(0, 1_000_000).ToString("D6"),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        _context.VerificationCodes.Add(verificationCode);
        await _context.SaveChangesAsync();
        return verificationCode;
    }

    public async Task<VerificationCode?> FindValidAsync(int id, string code, string purpose)
    {
        return await _context.VerificationCodes
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Code == code &&
                x.Purpose == purpose &&
                x.UsedAt == null &&
                x.ExpiresAt > DateTime.UtcNow);
    }

    public static string MaskDestination(string destination)
    {
        if (destination.Contains('@'))
        {
            var parts = destination.Split('@', 2);
            var name = parts[0].Length <= 2 ? parts[0] : parts[0][..2] + "***";
            return $"{name}@{parts[1]}";
        }

        return destination.Length <= 4 ? destination : "***" + destination[^4..];
    }
}
