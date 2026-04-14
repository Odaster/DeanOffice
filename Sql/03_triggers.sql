-- 1. Проверка корректности оценки перед вставкой/обновлением.
CREATE OR REPLACE FUNCTION trg_validate_grade()
RETURNS trigger AS $$
BEGIN
    IF NEW."Value" < 0 OR NEW."Value" > 10 THEN
        RAISE EXCEPTION 'Оценка должна быть в диапазоне от 0 до 10';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TR_Grades_Validate" ON "Grades";
CREATE TRIGGER "TR_Grades_Validate"
BEFORE INSERT OR UPDATE ON "Grades"
FOR EACH ROW EXECUTE FUNCTION trg_validate_grade();

-- 2. Логирование изменения оценки.
CREATE OR REPLACE FUNCTION trg_log_grade_change()
RETURNS trigger AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO "AuditLogs" ("UserId", "Action", "EntityName", "EntityId", "Timestamp", "Details")
        VALUES (NULL, 'SQL INSERT', 'Grade', NEW."Id"::text, now(), 'Добавлена оценка: ' || NEW."Value");
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO "AuditLogs" ("UserId", "Action", "EntityName", "EntityId", "Timestamp", "Details")
        VALUES (NULL, 'SQL UPDATE', 'Grade', NEW."Id"::text, now(), 'Оценка изменена с ' || OLD."Value" || ' на ' || NEW."Value");
        RETURN NEW;
    ELSE
        INSERT INTO "AuditLogs" ("UserId", "Action", "EntityName", "EntityId", "Timestamp", "Details")
        VALUES (NULL, 'SQL DELETE', 'Grade', OLD."Id"::text, now(), 'Удалена оценка: ' || OLD."Value");
        RETURN OLD;
    END IF;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TR_Grades_LogChange" ON "Grades";
CREATE TRIGGER "TR_Grades_LogChange"
AFTER INSERT OR UPDATE OR DELETE ON "Grades"
FOR EACH ROW EXECUTE FUNCTION trg_log_grade_change();

-- 3. Логирование создания студента.
CREATE OR REPLACE FUNCTION trg_log_student_insert()
RETURNS trigger AS $$
BEGIN
    INSERT INTO "AuditLogs" ("UserId", "Action", "EntityName", "EntityId", "Timestamp", "Details")
    VALUES (NULL, 'SQL INSERT', 'Student', NEW."Id"::text, now(), 'Создан студент: ' || NEW."LastName" || ' ' || NEW."FirstName");
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TR_Students_LogInsert" ON "Students";
CREATE TRIGGER "TR_Students_LogInsert"
AFTER INSERT ON "Students"
FOR EACH ROW EXECUTE FUNCTION trg_log_student_insert();
