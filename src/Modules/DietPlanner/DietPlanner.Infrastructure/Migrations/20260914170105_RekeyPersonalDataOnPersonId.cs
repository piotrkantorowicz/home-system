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
        migrationBuilder.RenameIndex("idx_weight_entries_user_date", "idx_weight_entries_person_date", table: "weight_entries");

        migrationBuilder.RenameColumn("user_id", "water_intakes", "person_id");
        migrationBuilder.Sql("ALTER TABLE water_intakes ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_water_intakes_user_date", "idx_water_intakes_person_date", table: "water_intakes");

        migrationBuilder.RenameColumn("user_id", "user_profiles", "person_id");
        migrationBuilder.Sql("ALTER TABLE user_profiles ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_user_profiles_user", "idx_user_profiles_person", table: "user_profiles");

        migrationBuilder.RenameColumn("user_id", "user_goals", "person_id");
        migrationBuilder.Sql("ALTER TABLE user_goals ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_user_goals_user", "idx_user_goals_person", table: "user_goals");

        migrationBuilder.RenameColumn("user_id", "meal_schedule_configs", "person_id");
        migrationBuilder.Sql("ALTER TABLE meal_schedule_configs ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_meal_schedule_configs_user", "idx_meal_schedule_configs_person", table: "meal_schedule_configs");

        migrationBuilder.RenameColumn("user_id", "meal_entries", "person_id");
        migrationBuilder.Sql("ALTER TABLE meal_entries ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_meal_entries_user_date", "idx_meal_entries_person_date", table: "meal_entries");

        migrationBuilder.RenameColumn("user_id", "hydration_configs", "person_id");
        migrationBuilder.Sql("ALTER TABLE hydration_configs ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_hydration_configs_user", "idx_hydration_configs_person", table: "hydration_configs");

        migrationBuilder.RenameColumn("user_id", "diet_reminder_settings", "person_id");
        migrationBuilder.Sql("ALTER TABLE diet_reminder_settings ALTER COLUMN person_id TYPE uuid USING person_id::uuid;");
        migrationBuilder.RenameIndex("idx_diet_reminder_settings_user", "idx_diet_reminder_settings_person", table: "diet_reminder_settings");

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
        migrationBuilder.RenameIndex("idx_diet_reminder_settings_person", "idx_diet_reminder_settings_user", table: "diet_reminder_settings");
        migrationBuilder.Sql("ALTER TABLE hydration_configs ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "hydration_configs", "user_id");
        migrationBuilder.RenameIndex("idx_hydration_configs_person", "idx_hydration_configs_user", table: "hydration_configs");
        migrationBuilder.Sql("ALTER TABLE meal_entries ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "meal_entries", "user_id");
        migrationBuilder.RenameIndex("idx_meal_entries_person_date", "idx_meal_entries_user_date", table: "meal_entries");
        migrationBuilder.Sql("ALTER TABLE meal_schedule_configs ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "meal_schedule_configs", "user_id");
        migrationBuilder.RenameIndex("idx_meal_schedule_configs_person", "idx_meal_schedule_configs_user", table: "meal_schedule_configs");
        migrationBuilder.Sql("ALTER TABLE user_goals ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "user_goals", "user_id");
        migrationBuilder.RenameIndex("idx_user_goals_person", "idx_user_goals_user", table: "user_goals");
        migrationBuilder.Sql("ALTER TABLE user_profiles ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "user_profiles", "user_id");
        migrationBuilder.RenameIndex("idx_user_profiles_person", "idx_user_profiles_user", table: "user_profiles");
        migrationBuilder.Sql("ALTER TABLE water_intakes ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "water_intakes", "user_id");
        migrationBuilder.RenameIndex("idx_water_intakes_person_date", "idx_water_intakes_user_date", table: "water_intakes");
        migrationBuilder.Sql("ALTER TABLE weight_entries ALTER COLUMN person_id TYPE character varying(255) USING person_id::text;");
        migrationBuilder.RenameColumn("person_id", "weight_entries", "user_id");
        migrationBuilder.RenameIndex("idx_weight_entries_person_date", "idx_weight_entries_user_date", table: "weight_entries");
    }
}
