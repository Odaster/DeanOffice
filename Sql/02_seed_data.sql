INSERT INTO "Roles" ("Id", "Name") VALUES
(1, 'Admin'),
(2, 'Teacher'),
(3, 'Student');

INSERT INTO "Faculties" ("Id", "Name", "Code") VALUES
(1, 'Факультет информационных технологий', 'ФИТ'),
(2, 'Экономический факультет', 'ЭФ');

INSERT INTO "Specialties" ("Id", "Name", "Code", "FacultyId") VALUES
(1, 'Информационные системы и технологии', 'ИСиТ', 1),
(2, 'Прикладная информатика', 'ПИ', 1),
(3, 'Экономика', 'ЭК', 2);

INSERT INTO "Groups" ("Id", "Name", "Course", "YearOfAdmission", "SpecialtyId") VALUES
(1, 'ИС-21', 2, 2024, 1),
(2, 'ПИ-31', 3, 2023, 2),
(3, 'ЭК-11', 1, 2025, 3);

INSERT INTO "Students" ("Id", "LastName", "FirstName", "MiddleName", "StudentBookNumber", "BirthDate", "IsActive", "GroupId") VALUES
(1, 'Иванов', 'Иван', 'Иванович', '2024-001', '2005-05-12', true, 1),
(2, 'Петрова', 'Анна', 'Сергеевна', '2024-002', '2005-09-03', true, 1),
(3, 'Смирнов', 'Олег', 'Павлович', '2023-010', '2004-02-21', true, 2),
(4, 'Кузнецова', 'Мария', 'Игоревна', '2025-006', '2006-11-18', true, 3);

INSERT INTO "Teachers" ("Id", "LastName", "FirstName", "MiddleName", "Department", "Email") VALUES
(1, 'Сидоров', 'Петр', 'Алексеевич', 'Кафедра программирования', 'sidorov@example.edu'),
(2, 'Морозова', 'Елена', 'Викторовна', 'Кафедра математики', 'morozova@example.edu');

INSERT INTO "Users" ("Id", "UserName", "PasswordHash", "FullName", "Email", "PhoneNumber", "EmailConfirmed", "PhoneNumberConfirmed", "RoleId", "StudentId", "TeacherId") VALUES
(1, 'admin', 'admin123', 'Администратор', 'admin@example.edu', '+375291111111', true, true, 1, NULL, NULL),
(2, 'teacher', 'teacher123', 'Сидоров Петр Алексеевич', 'sidorov@example.edu', '+375292222222', true, false, 2, NULL, 1),
(3, 'student', 'student123', 'Иванов Иван Иванович', 'student@example.edu', '+375293333333', false, false, 3, 1, NULL);

INSERT INTO "Subjects" ("Id", "Name", "Code", "Hours") VALUES
(1, 'Базы данных', 'DB', 96),
(2, 'Объектно-ориентированное программирование', 'OOP', 120),
(3, 'Математический анализ', 'MATH', 108);

INSERT INTO "Semesters" ("Id", "Name", "Number", "AcademicYear", "StartDate", "EndDate") VALUES
(1, 'Весенний семестр', 4, '2025/2026', '2026-02-01', '2026-06-30'),
(2, 'Осенний семестр', 5, '2026/2027', '2026-09-01', '2026-12-31');

INSERT INTO "GroupSubjects" ("Id", "GroupId", "SubjectId", "TeacherId", "SemesterId") VALUES
(1, 1, 1, 1, 1),
(2, 1, 2, 1, 1),
(3, 2, 3, 2, 1),
(4, 3, 3, 2, 1);

INSERT INTO "Grades" ("Id", "StudentId", "GroupSubjectId", "Value", "GradeType", "GradeDate", "Comment") VALUES
(1, 1, 1, 8, 'Экзамен', '2026-04-10', 'Хорошая работа'),
(2, 2, 1, 3, 'Экзамен', '2026-04-10', 'Нужна пересдача'),
(3, 1, 2, 9, 'Зачет', '2026-04-11', ''),
(4, 3, 3, 5, 'Экзамен', '2026-04-12', '');

INSERT INTO "Attendance" ("Id", "StudentId", "GroupSubjectId", "LessonDate", "IsPresent", "Comment") VALUES
(1, 1, 1, '2026-04-01', true, ''),
(2, 2, 1, '2026-04-01', false, 'Отсутствовала'),
(3, 1, 2, '2026-04-02', true, ''),
(4, 2, 2, '2026-04-02', true, ''),
(5, 3, 3, '2026-04-03', false, '');

SELECT setval(pg_get_serial_sequence('"Roles"', 'Id'), (SELECT MAX("Id") FROM "Roles"));
SELECT setval(pg_get_serial_sequence('"Faculties"', 'Id'), (SELECT MAX("Id") FROM "Faculties"));
SELECT setval(pg_get_serial_sequence('"Specialties"', 'Id'), (SELECT MAX("Id") FROM "Specialties"));
SELECT setval(pg_get_serial_sequence('"Groups"', 'Id'), (SELECT MAX("Id") FROM "Groups"));
SELECT setval(pg_get_serial_sequence('"Students"', 'Id'), (SELECT MAX("Id") FROM "Students"));
SELECT setval(pg_get_serial_sequence('"Teachers"', 'Id'), (SELECT MAX("Id") FROM "Teachers"));
SELECT setval(pg_get_serial_sequence('"Users"', 'Id'), (SELECT MAX("Id") FROM "Users"));
SELECT setval(pg_get_serial_sequence('"Subjects"', 'Id'), (SELECT MAX("Id") FROM "Subjects"));
SELECT setval(pg_get_serial_sequence('"Semesters"', 'Id'), (SELECT MAX("Id") FROM "Semesters"));
SELECT setval(pg_get_serial_sequence('"GroupSubjects"', 'Id'), (SELECT MAX("Id") FROM "GroupSubjects"));
SELECT setval(pg_get_serial_sequence('"Grades"', 'Id'), (SELECT MAX("Id") FROM "Grades"));
SELECT setval(pg_get_serial_sequence('"Attendance"', 'Id'), (SELECT MAX("Id") FROM "Attendance"));
