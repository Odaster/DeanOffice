-- 1. Средний балл каждого студента.
SELECT s."Id", s."LastName" || ' ' || s."FirstName" AS student_name, g."Name" AS group_name,
       ROUND(AVG(gr."Value")::numeric, 2) AS average_grade
FROM "Students" s
JOIN "Groups" g ON g."Id" = s."GroupId"
LEFT JOIN "Grades" gr ON gr."StudentId" = s."Id"
GROUP BY s."Id", student_name, g."Name"
ORDER BY g."Name", student_name;

-- 2. Студенты, у которых средний балл выше среднего по всей базе.
SELECT s."Id", s."LastName" || ' ' || s."FirstName" AS student_name, ROUND(AVG(gr."Value")::numeric, 2) AS average_grade
FROM "Students" s
JOIN "Grades" gr ON gr."StudentId" = s."Id"
GROUP BY s."Id", student_name
HAVING AVG(gr."Value") > (SELECT AVG("Value") FROM "Grades");

-- 3. Количество студентов по факультетам.
SELECT f."Name" AS faculty, COUNT(s."Id") AS students_count
FROM "Faculties" f
JOIN "Specialties" sp ON sp."FacultyId" = f."Id"
JOIN "Groups" g ON g."SpecialtyId" = sp."Id"
LEFT JOIN "Students" s ON s."GroupId" = g."Id"
GROUP BY f."Name"
ORDER BY students_count DESC;

-- 4. Задолженности студентов по дисциплинам.
SELECT s."LastName" || ' ' || s."FirstName" AS student_name, g."Name" AS group_name, sub."Name" AS subject_name, gr."Value"
FROM "Grades" gr
JOIN "Students" s ON s."Id" = gr."StudentId"
JOIN "Groups" g ON g."Id" = s."GroupId"
JOIN "GroupSubjects" gs ON gs."Id" = gr."GroupSubjectId"
JOIN "Subjects" sub ON sub."Id" = gs."SubjectId"
WHERE gr."Value" < 4
ORDER BY g."Name", student_name;

-- 5. Посещаемость по группам и дисциплинам.
SELECT g."Name" AS group_name, sub."Name" AS subject_name,
       COUNT(a."Id") AS lessons_count,
       COUNT(a."Id") FILTER (WHERE a."IsPresent") AS present_count,
       ROUND(COUNT(a."Id") FILTER (WHERE a."IsPresent") * 100.0 / NULLIF(COUNT(a."Id"), 0), 2) AS attendance_percent
FROM "Attendance" a
JOIN "GroupSubjects" gs ON gs."Id" = a."GroupSubjectId"
JOIN "Groups" g ON g."Id" = gs."GroupId"
JOIN "Subjects" sub ON sub."Id" = gs."SubjectId"
GROUP BY g."Name", sub."Name";

-- 6. Преподаватели и количество закрепленных дисциплин.
SELECT t."LastName" || ' ' || t."FirstName" AS teacher_name, COUNT(DISTINCT gs."SubjectId") AS subjects_count
FROM "Teachers" t
LEFT JOIN "GroupSubjects" gs ON gs."TeacherId" = t."Id"
GROUP BY t."Id", teacher_name
ORDER BY subjects_count DESC;

-- 7. Группы, где есть хотя бы один должник.
SELECT g."Name", COUNT(DISTINCT s."Id") AS debtors_count
FROM "Groups" g
JOIN "Students" s ON s."GroupId" = g."Id"
WHERE EXISTS (SELECT 1 FROM "Grades" gr WHERE gr."StudentId" = s."Id" AND gr."Value" < 4)
GROUP BY g."Name";

-- 8. Дисциплины со средней оценкой ниже 6.
SELECT sub."Name", ROUND(AVG(gr."Value")::numeric, 2) AS average_grade
FROM "Subjects" sub
JOIN "GroupSubjects" gs ON gs."SubjectId" = sub."Id"
JOIN "Grades" gr ON gr."GroupSubjectId" = gs."Id"
GROUP BY sub."Name"
HAVING AVG(gr."Value") < 6;

-- 9. Студенты без оценок по назначенным дисциплинам своей группы.
SELECT s."LastName" || ' ' || s."FirstName" AS student_name, g."Name" AS group_name, sub."Name" AS subject_name
FROM "Students" s
JOIN "Groups" g ON g."Id" = s."GroupId"
JOIN "GroupSubjects" gs ON gs."GroupId" = g."Id"
JOIN "Subjects" sub ON sub."Id" = gs."SubjectId"
WHERE NOT EXISTS (
    SELECT 1 FROM "Grades" gr
    WHERE gr."StudentId" = s."Id" AND gr."GroupSubjectId" = gs."Id"
);

-- 10. Активность пользователей в журнале.
SELECT u."UserName", r."Name" AS role_name, COUNT(al."Id") AS actions_count, MAX(al."Timestamp") AS last_action
FROM "Users" u
JOIN "Roles" r ON r."Id" = u."RoleId"
LEFT JOIN "AuditLogs" al ON al."UserId" = u."Id"
GROUP BY u."UserName", r."Name"
ORDER BY actions_count DESC;
