# База данных организации деятельности деканата

Учебное ASP.NET Core MVC-приложение для курсовой работы. Используются C#, ASP.NET Core MVC, Entity Framework Core и PostgreSQL.

## Что реализовано

- 13 таблиц предметной области: Roles, Users, Faculties, Specialties, Groups, Students, Teachers, Subjects, Semesters, GroupSubjects, Grades, Attendance, AuditLogs.
- Роли Admin, Teacher, Student через простую cookie-аутентификацию.
- CRUD для Students, Groups, Teachers, Subjects, Grades.
- Дополнительные страницы Attendance, Reports, AuditLogs, AdminQueryBuilder, BackupRestore, Users.
- 3 отчета с просмотром, печатью, CSV и Excel-совместимым экспортом.
- SQL-конструктор для Admin только для безопасных SELECT-запросов.
- Резервное копирование через pg_dump и восстановление через pg_restore.
- SQL-скрипты с триггерами, функциями, процедурами и 10 сложными запросами.

## Запуск

1. Установите PostgreSQL и проверьте, что команды `pg_dump` и `pg_restore` доступны из PATH.
2. В PostgreSQL выполните скрипты из папки `Sql` по порядку:
   - `01_create_database.sql`
   - `02_seed_data.sql`
   - `03_triggers.sql`
   - `04_functions.sql`
   - `05_procedures.sql`
   - `07_account_profile_features.sql`
3. Проверьте строку подключения в `appsettings.json`.
4. Запустите приложение:

```powershell
dotnet run
```

## Демо-пользователи

| Роль | Логин | Пароль |
| --- | --- | --- |
| Admin | admin | admin123 |
| Teacher | teacher | teacher123 |
| Student | student | student123 |

## Отправка кодов подтверждения

Для реальной отправки кодов заполните блок `Notifications` в `appsettings.json` или через user secrets.

Email отправляется через SMTP:

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "Username": "your-email@gmail.com",
  "Password": "app-password",
  "From": "your-email@gmail.com",
  "EnableSsl": "true"
}
```

SMS по умолчанию работает в бесплатном учебном режиме. Код сохраняется в `VerificationCodes` и выводится на странице как тестовое SMS-сообщение:

```json
"Sms": {
  "Provider": "Test"
}
```

Если понадобится реальная отправка SMS, можно подключить Twilio и переключить провайдер:

```json
"Sms": {
  "Provider": "Twilio"
},
"Twilio": {
  "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "AuthToken": "your-auth-token",
  "From": "+1234567890"
}
```

Телефон пользователя должен быть в международном формате, например `+375291111111`.

## Плановое резервное копирование

Windows Task Scheduler может запускать команду:

```powershell
set PGPASSWORD=postgres && pg_dump --format=custom --file C:\Backups\DeanOfficeDb_%DATE%.backup --host localhost --port 5432 --username postgres --dbname DeanOfficeDb
```

Пример cron для Linux:

```bash
0 2 * * * PGPASSWORD=postgres pg_dump --format=custom --file /var/backups/DeanOfficeDb_$(date +\%Y\%m\%d).backup --host localhost --port 5432 --username postgres --dbname DeanOfficeDb
```
