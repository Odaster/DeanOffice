CREATE OR REPLACE FUNCTION get_student_average_grade(p_student_id integer)
RETURNS numeric AS $$
BEGIN
    RETURN COALESCE((SELECT ROUND(AVG("Value")::numeric, 2) FROM "Grades" WHERE "StudentId" = p_student_id), 0);
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION count_student_debts(p_student_id integer)
RETURNS integer AS $$
BEGIN
    RETURN (SELECT COUNT(*) FROM "Grades" WHERE "StudentId" = p_student_id AND "Value" < 4);
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION get_student_attendance_percent(p_student_id integer)
RETURNS numeric AS $$
DECLARE
    total_count integer;
    present_count integer;
BEGIN
    SELECT COUNT(*), COUNT(*) FILTER (WHERE "IsPresent")
    INTO total_count, present_count
    FROM "Attendance"
    WHERE "StudentId" = p_student_id;

    IF total_count = 0 THEN
        RETURN 0;
    END IF;

    RETURN ROUND(present_count * 100.0 / total_count, 2);
END;
$$ LANGUAGE plpgsql;
