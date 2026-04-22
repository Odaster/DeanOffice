# Docker-развертывание DeanOfficeCourseWork

Комплект запускает два контейнера:

- `deanoffice-app` - ASP.NET Core MVC приложение;
- `deanoffice-postgres` - PostgreSQL с базой `DeanOfficeDb`.

## Требования

На компьютере должен быть установлен Docker Desktop с поддержкой `docker compose`.

Проверка:

```powershell
docker --version
docker compose version
```

Если Docker Desktop только что установлен, перезапустите Windows. Это нужно для применения WSL/Virtual Machine Platform и обновления переменной PATH.

## Запуск

Из каталога проекта:

```powershell
cd "F:\Visual Studio\DeanOfficeCourseWork"
docker compose up --build
```

Если команда `docker` еще не видна в PowerShell после установки Docker Desktop, используйте готовый скрипт:

```powershell
.\docker-up.ps1
```

После запуска открыть:

```text
http://localhost:5157
```

## Учетные записи

```text
admin / admin123
teacher / teacher123
student / student123
```

## Подключение к PostgreSQL с хоста

Контейнер PostgreSQL проброшен на порт `5433`, чтобы не конфликтовать с локальным PostgreSQL на `5432`.

```text
Host: localhost
Port: 5433
Database: DeanOfficeDb
Username: postgres
Password: postgres
```

Внутри Docker-сети приложение подключается к БД по адресу `db:5432`.

## Данные и резервные копии

Данные PostgreSQL хранятся в Docker volume:

```text
deanoffice_postgres_data
```

Резервные копии приложения сохраняются в локальную папку проекта:

```text
Backups
```

Фотографии профиля сохраняются в:

```text
wwwroot/uploads/profiles
```

## Полная пересборка с очисткой БД

Если нужно заново создать базу из SQL-скриптов:

```powershell
docker compose down -v
docker compose up --build
```

Ключ `-v` удаляет volume PostgreSQL, поэтому все данные базы будут потеряны.

## Остановка

```powershell
docker compose down
```

Или:

```powershell
.\docker-down.ps1
```

## SQL-инициализация

Первичное создание БД выполняется скриптами:

```text
docker/postgres/init/01_schema.sql
docker/postgres/init/02_seed_data.sql
docker/postgres/init/03_functions.sql
docker/postgres/init/04_procedures.sql
docker/postgres/init/05_triggers.sql
```

Эти скрипты автоматически выполняются официальным образом PostgreSQL только при первом создании пустого volume.
