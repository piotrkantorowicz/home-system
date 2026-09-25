namespace DietPlanner.Infrastructure.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class RekeyPersonalDataOnPersonId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Pre-release migration: no subject-to-person mapping exists. Refuse to discard
        // existing personal history; developers must explicitly recreate their database.
        migrationBuilder.Sql("""
            DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM weight_entries)
                    OR EXISTS (SELECT 1 FROM water_intakes)
                    OR EXISTS (SELECT 1 FROM user_profiles)
                    OR EXISTS (SELECT 1 FROM user_goals)
                    OR EXISTS (SELECT 1 FROM meal_schedule_configs)
                    OR EXISTS (SELECT 1 FROM meal_entries)
                    OR EXISTS (SELECT 1 FROM hydration_configs)
                    OR EXISTS (SELECT 1 FROM diet_reminder_settings)
                    OR EXISTS (SELECT 1 FROM weekly_summary_state)
                    OR EXISTS (SELECT 1 FROM water_reminder_state) THEN
                    RAISE EXCEPTION 'PersonId migration requires empty personal tables. Recreate the disposable development database explicitly; no data is deleted by this migration.';
                END IF;
            END $$;
            """);

        migrationBuilder.RenameColumn("user_id", "weight_entries", "person_id");
        migrationBuilder.Sql("ALTER TABLE weight_entries ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_weight_entries_user_date", "weight_entries", "idx_weight_entries_person_date");

        migrationBuilder.RenameColumn("user_id", "water_intakes", "person_id");
        migrationBuilder.Sql("ALTER TABLE water_intakes ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_water_intakes_user_date", "water_intakes", "idx_water_intakes_person_date");

        migrationBuilder.RenameColumn("user_id", "user_profiles", "person_id");
        migrationBuilder.Sql("ALTER TABLE user_profiles ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_user_profiles_user", "user_profiles", "idx_user_profiles_person");

        migrationBuilder.RenameColumn("user_id", "user_goals", "person_id");
        migrationBuilder.Sql("ALTER TABLE user_goals ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_user_goals_user", "user_goals", "idx_user_goals_person");

        migrationBuilder.RenameColumn("user_id", "meal_schedule_configs", "person_id");
        migrationBuilder.Sql("ALTER TABLE meal_schedule_configs ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_meal_schedule_configs_user", "meal_schedule_configs", "idx_meal_schedule_configs_person");

        migrationBuilder.RenameColumn("user_id", "meal_entries", "person_id");
        migrationBuilder.Sql("ALTER TABLE meal_entries ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_meal_entries_user_date", "meal_entries", "idx_meal_entries_person_date");

        migrationBuilder.RenameColumn("user_id", "hydration_configs", "person_id");
        migrationBuilder.Sql("ALTER TABLE hydration_configs ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_hydration_configs_user", "hydration_configs", "idx_hydration_configs_person");

        migrationBuilder.RenameColumn("user_id", "diet_reminder_settings", "person_id");
        migrationBuilder.Sql("ALTER TABLE diet_reminder_settings ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_diet_reminder_settings_user", "diet_reminder_settings", "idx_diet_reminder_settings_person");

        migrationBuilder.RenameColumn("user_id", "weekly_summary_state", "person_id");
        migrationBuilder.Sql("ALTER TABLE weekly_summary_state ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");

        migrationBuilder.RenameColumn("user_id", "water_reminder_state", "person_id");
        migrationBuilder.Sql("ALTER TABLE water_reminder_state ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE water_reminder_state ALTER COLUMN person_id TYPE text USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "water_reminder_state", "user_id");
        migrationBuilder.Sql("ALTER TABLE weekly_summary_state ALTER COLUMN person_id TYPE text USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "weekly_summary_state", "user_id");
        migrationBuilder.Sql("ALTER TABLE diet_reminder_settings ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "diet_reminder_settings", "user_id");
        migrationBuilder.RenameIndex("idx_diet_reminder_settings_person", "diet_reminder_settings", "idx_diet_reminder_settings_user");
        migrationBuilder.Sql("ALTER TABLE hydration_configs ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "hydration_configs", "user_id");
        migrationBuilder.RenameIndex("idx_hydration_configs_person", "hydration_configs", "idx_hydration_configs_user");
        migrationBuilder.Sql("ALTER TABLE meal_entries ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "meal_entries", "user_id");
        migrationBuilder.RenameIndex("idx_meal_entries_person_date", "meal_entries", "idx_meal_entries_user_date");
        migrationBuilder.Sql("ALTER TABLE meal_schedule_configs ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "meal_schedule_configs", "user_id");
        migrationBuilder.RenameIndex("idx_meal_schedule_configs_person", "meal_schedule_configs", "idx_meal_schedule_configs_user");
        migrationBuilder.Sql("ALTER TABLE user_goals ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "user_goals", "user_id");
        migrationBuilder.RenameIndex("idx_user_goals_person", "user_goals", "idx_user_goals_user");
        migrationBuilder.Sql("ALTER TABLE user_profiles ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "user_profiles", "user_id");
        migrationBuilder.RenameIndex("idx_user_profiles_person", "user_profiles", "idx_user_profiles_user");
        migrationBuilder.Sql("ALTER TABLE water_intakes ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "water_intakes", "user_id");
        migrationBuilder.RenameIndex("idx_water_intakes_person_date", "water_intakes", "idx_water_intakes_user_date");
        migrationBuilder.Sql("ALTER TABLE weight_entries ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "weight_entries", "user_id");
        migrationBuilder.RenameIndex("idx_weight_entries_person_date", "weight_entries", "idx_weight_entries_user_date");
    }
}
