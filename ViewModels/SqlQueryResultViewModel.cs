namespace DeanOfficeCourseWork.ViewModels;

public class SqlQueryResultViewModel
{
    public string Query { get; set; } = "SELECT \"Id\", \"LastName\", \"FirstName\" FROM \"Students\" LIMIT 10;";
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
