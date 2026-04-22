CREATE OR REPLACE PROCEDURE promote_students_next_course()
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE "Groups"
    SET "Course" = "Course" + 1
    WHERE "Course" < 6
      AND EXISTS (SELECT 1 FROM "Students" s WHERE s."GroupId" = "Groups"."Id" AND s."IsActive");
END;
$$;

CREATE OR REPLACE PROCEDURE create_attendance_for_group(
    p_group_id integer,
    p_group_subject_id integer,
    p_lesson_date date
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO "Attendance" ("StudentId", "GroupSubjectId", "LessonDate", "IsPresent", "Comment")
    SELECT s."Id", p_group_subject_id, p_lesson_date, true, 'Создано процедурой'
    FROM "Students" s
    WHERE s."GroupId" = p_group_id
      AND s."IsActive"
      AND NOT EXISTS (
          SELECT 1 FROM "Attendance" a
          WHERE a."StudentId" = s."Id"
            AND a."GroupSubjectId" = p_group_subject_id
            AND a."LessonDate"::date = p_lesson_date
      );
END;
$$;

CREATE OR REPLACE PROCEDURE build_debtors_list()
LANGUAGE plpgsql
AS $$
BEGIN
    DROP TABLE IF EXISTS temp_debtors;
    CREATE TEMP TABLE temp_debtors AS
    SELECT s."Id" AS student_id,
           s."LastName" || ' ' || s."FirstName" AS student_name,
           g."Name" AS group_name,
           sub."Name" AS subject_name,
           gr."Value" AS grade_value
    FROM "Grades" gr
    JOIN "Students" s ON s."Id" = gr."StudentId"
    JOIN "Groups" g ON g."Id" = s."GroupId"
    JOIN "GroupSubjects" gs ON gs."Id" = gr."GroupSubjectId"
    JOIN "Subjects" sub ON sub."Id" = gs."SubjectId"
    WHERE gr."Value" < 4;
END;
$$;
