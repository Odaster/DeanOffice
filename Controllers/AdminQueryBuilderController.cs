using System.Data;
using System.Text.RegularExpressions;
using DeanOfficeCourseWork.Data;
using DeanOfficeCourseWork.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeanOfficeCourseWork.Controllers;

[Authorize(Roles = "Admin")]
public class AdminQueryBuilderController : Controller
{
    private static readonly Regex DangerousWords = new("(insert|update|delete|drop|alter|truncate|create|grant|revoke|execute|call|copy)\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly ApplicationDbContext _context;

    public AdminQueryBuilderController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View(new SqlQueryResultViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SqlQueryResultViewModel model)
    {
        var query = model.Query.Trim();
        var normalizedQuery = query.EndsWith(';') ? query[..^1] : query;
        if (!normalizedQuery.StartsWith("select", StringComparison.OrdinalIgnoreCase) || DangerousWords.IsMatch(normalizedQuery) || normalizedQuery.Contains(';'))
        {
            model.ErrorMessage = "Разрешены только безопасные SELECT-запросы без команд изменения данных.";
            return View(model);
        }

        await using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = normalizedQuery;
        command.CommandTimeout = 30;

        try
        {
            await using var reader = await command.ExecuteReaderAsync();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                model.Columns.Add(reader.GetName(i));
            }

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                foreach (var column in model.Columns)
                {
                    var ordinal = reader.GetOrdinal(column);
                    row[column] = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
                }

                model.Rows.Add(row);
            }
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        return View(model);
    }
}
