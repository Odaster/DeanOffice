using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Globalization;
using DeanOfficeCourseWork.ViewModels;
using Npgsql;

namespace DeanOfficeCourseWork.Services;

public class BackupService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public BackupService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public string BackupDirectory
    {
        get
        {
            var configured = _configuration["Backup:Folder"];
            return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configured ?? "Backups"));
        }
    }

    public List<BackupFileViewModel> GetBackups()
    {
        Directory.CreateDirectory(BackupDirectory);

        return Directory.GetFiles(BackupDirectory, "*.backup")
            .Select(path =>
            {
                var info = new FileInfo(path);
                return new BackupFileViewModel
                {
                    FileName = info.Name,
                    SizeBytes = info.Length,
                    CreatedAt = info.CreationTime
                };
            })
            .Where(x => x.SizeBytes > 0)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
    }

    public async Task<string> CreateBackupAsync()
    {
        // Для планового копирования используйте эту же команду pg_dump через Windows Task Scheduler или cron.
        // Пример есть в README.md и на странице BackupRestore.
        Directory.CreateDirectory(BackupDirectory);

        var fileName = $"dean_office_{DateTime.Now:yyyyMMdd_HHmmss}.backup";
        var fullPath = Path.Combine(BackupDirectory, fileName);
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string DefaultConnection is missing.");
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var pgDumpPath = _configuration["Backup:PgDumpPath"] ?? "pg_dump";

        try
        {
            await RunProcessAsync(pgDumpPath, args =>
            {
                args.Add("--format=custom");
                args.Add("--file");
                args.Add(fullPath);
                AddConnectionArguments(args, connection);
            }, connection.Password);
        }
        catch
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            throw;
        }

        return fileName;
    }

    public async Task RestoreAsync(string fileName)
    {
        if (fileName.Contains("..") || Path.GetFileName(fileName) != fileName)
        {
            throw new InvalidOperationException("Некорректное имя файла резервной копии.");
        }

        var fullPath = Path.Combine(BackupDirectory, fileName);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Резервная копия не найдена.", fullPath);
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string DefaultConnection is missing.");
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var pgRestorePath = _configuration["Backup:PgRestorePath"] ?? "pg_restore";

        await RunProcessAsync(pgRestorePath, args =>
        {
            args.Add("--clean");
            args.Add("--if-exists");
            AddConnectionArguments(args, connection);
            args.Add(fullPath);
        }, connection.Password);
    }

    public async Task<string> CreateScheduledBackupTaskAsync(string startTime)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException("Плановое резервное копирование через Windows Task Scheduler доступно только в Windows.");
        }

        if (!TimeOnly.TryParseExact(startTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime))
        {
            throw new InvalidOperationException("Время запуска должно быть указано в формате HH:mm, например 02:00.");
        }

        Directory.CreateDirectory(BackupDirectory);

        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string DefaultConnection is missing.");
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var pgDumpPath = _configuration["Backup:PgDumpPath"] ?? "pg_dump";
        var scriptPath = Path.Combine(BackupDirectory, "scheduled_backup.ps1");
        var taskName = "DeanOfficeCourseWork Backup";

        var script = BuildScheduledBackupScript(pgDumpPath, connection);
        await File.WriteAllTextAsync(scriptPath, script);

        var taskCommand = $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"";
        await RunProcessAsync("schtasks.exe", args =>
        {
            args.Add("/Create");
            args.Add("/TN");
            args.Add(taskName);
            args.Add("/SC");
            args.Add("HOURLY");
            args.Add("/MO");
            args.Add("4");
            args.Add("/ST");
            args.Add(parsedTime.ToString("HH:mm", CultureInfo.InvariantCulture));
            args.Add("/TR");
            args.Add(taskCommand);
            args.Add("/RL");
            args.Add("LIMITED");
            args.Add("/F");
        }, null);

        return taskName;
    }

    private string BuildScheduledBackupScript(string pgDumpPath, NpgsqlConnectionStringBuilder connection)
    {
        if (string.IsNullOrWhiteSpace(connection.Database))
        {
            throw new InvalidOperationException("В строке подключения не указано имя базы данных.");
        }

        static string PsQuote(string? value) => "'" + (value ?? string.Empty).Replace("'", "''") + "'";

        var host = string.IsNullOrWhiteSpace(connection.Host) ? "localhost" : connection.Host;
        var port = connection.Port > 0 ? connection.Port.ToString(CultureInfo.InvariantCulture) : "5432";
        var username = string.IsNullOrWhiteSpace(connection.Username) ? "postgres" : connection.Username;

        return $$"""
$ErrorActionPreference = 'Stop'
$env:PGPASSWORD = {{PsQuote(connection.Password)}}
$backupDirectory = {{PsQuote(BackupDirectory)}}
New-Item -ItemType Directory -Force -Path $backupDirectory | Out-Null
$file = Join-Path $backupDirectory ("dean_office_scheduled_{0}.backup" -f (Get-Date -Format 'yyyyMMdd_HHmmss'))
& {{PsQuote(pgDumpPath)}} --format=custom --file $file --host {{PsQuote(host)}} --port {{PsQuote(port)}} --username {{PsQuote(username)}} --dbname {{PsQuote(connection.Database)}}
if ($LASTEXITCODE -ne 0) { throw "pg_dump завершился с кодом $LASTEXITCODE" }
""";
    }

    private static void AddConnectionArguments(Collection<string> args, NpgsqlConnectionStringBuilder connection)
    {
        if (string.IsNullOrWhiteSpace(connection.Database))
        {
            throw new InvalidOperationException("В строке подключения не указано имя базы данных.");
        }

        args.Add("--host");
        args.Add(string.IsNullOrWhiteSpace(connection.Host) ? "localhost" : connection.Host);
        args.Add("--port");
        args.Add(connection.Port > 0 ? connection.Port.ToString() : "5432");
        args.Add("--username");
        args.Add(string.IsNullOrWhiteSpace(connection.Username) ? "postgres" : connection.Username);
        args.Add("--dbname");
        args.Add(connection.Database);
    }

    private static async Task RunProcessAsync(string fileName, Action<Collection<string>> configureArguments, string? password)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        configureArguments(startInfo.ArgumentList);
        if (!string.IsNullOrWhiteSpace(password))
        {
            startInfo.Environment["PGPASSWORD"] = password;
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Не удалось запустить {fileName}.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} завершился с кодом {process.ExitCode}. {error} {output}");
        }
    }
}
