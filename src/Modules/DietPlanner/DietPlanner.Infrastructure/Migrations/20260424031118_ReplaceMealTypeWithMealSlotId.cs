using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceMealTypeWithMealSlotId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add meal_slot_id as nullable so we can backfill before locking down.
            migrationBuilder.Sql("ALTER TABLE meal_entries ADD COLUMN meal_slot_id uuid;");

            // 2. Auto-provision a default MealScheduleConfig for any user that has
            //    meal entries but no schedule yet.
            migrationBuilder.Sql(@"
                INSERT INTO meal_schedule_configs (id, user_id, created_at)
                SELECT gen_random_uuid(), u.user_id, NOW() AT TIME ZONE 'UTC'
                FROM (SELECT DISTINCT user_id FROM meal_entries) u
                WHERE NOT EXISTS (
                    SELECT 1 FROM meal_schedule_configs c WHERE c.user_id = u.user_id
                );
            ");

            // 3. Seed default slots for any config that was just created (i.e. has no slots).
            migrationBuilder.Sql(@"
                INSERT INTO meal_slots (id, meal_schedule_config_id, name, default_time, sort_order)
                SELECT gen_random_uuid(), c.id, slot.name, slot.default_time, slot.sort_order
                FROM meal_schedule_configs c
                CROSS JOIN (VALUES
                    ('Breakfast', TIME '07:00', 0),
                    ('Lunch',     TIME '12:00', 1),
                    ('Dinner',    TIME '18:00', 2),
                    ('Snack',     TIME '15:00', 3)
                ) AS slot(name, default_time, sort_order)
                WHERE NOT EXISTS (
                    SELECT 1 FROM meal_slots s WHERE s.meal_schedule_config_id = c.id
                );
            ");

            // 4. Best-effort backfill: case-insensitive name match against the user's slots.
            migrationBuilder.Sql(@"
                UPDATE meal_entries e
                SET meal_slot_id = s.id
                FROM meal_schedule_configs c
                JOIN meal_slots s ON s.meal_schedule_config_id = c.id
                WHERE e.user_id = c.user_id
                  AND lower(s.name) = lower(e.meal_type);
            ");

            // 5. For any orphans, ensure an 'Other' slot exists per user, then assign.
            migrationBuilder.Sql(@"
                INSERT INTO meal_slots (id, meal_schedule_config_id, name, default_time, sort_order)
                SELECT gen_random_uuid(),
                       c.id,
                       'Other',
                       TIME '12:00',
                       (SELECT COALESCE(MAX(sort_order), -1) + 1
                        FROM meal_slots
                        WHERE meal_schedule_config_id = c.id)
                FROM meal_schedule_configs c
                WHERE EXISTS (
                    SELECT 1 FROM meal_entries e
                    WHERE e.user_id = c.user_id AND e.meal_slot_id IS NULL
                )
                AND NOT EXISTS (
                    SELECT 1 FROM meal_slots s
                    WHERE s.meal_schedule_config_id = c.id
                      AND lower(s.name) = 'other'
                );
            ");

            migrationBuilder.Sql(@"
                UPDATE meal_entries e
                SET meal_slot_id = s.id
                FROM meal_schedule_configs c
                JOIN meal_slots s
                  ON s.meal_schedule_config_id = c.id
                 AND lower(s.name) = 'other'
                WHERE e.user_id = c.user_id
                  AND e.meal_slot_id IS NULL;
            ");

            // 6. Lock down the schema: NOT NULL, FK, index. Drop the legacy column.
            migrationBuilder.Sql("ALTER TABLE meal_entries ALTER COLUMN meal_slot_id SET NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "idx_meal_entries_meal_slot",
                table: "meal_entries",
                column: "meal_slot_id");

            migrationBuilder.AddForeignKey(
                name: "FK_meal_entries_meal_slots_meal_slot_id",
                table: "meal_entries",
                column: "meal_slot_id",
                principalTable: "meal_slots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "meal_type",
                table: "meal_entries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_meal_entries_meal_slots_meal_slot_id",
                table: "meal_entries");

            migrationBuilder.DropIndex(
                name: "idx_meal_entries_meal_slot",
                table: "meal_entries");

            // Restore meal_type and best-effort populate from current slot name.
            migrationBuilder.Sql(@"
                ALTER TABLE meal_entries
                ADD COLUMN meal_type character varying(50);
            ");

            migrationBuilder.Sql(@"
                UPDATE meal_entries e
                SET meal_type = lower(s.name)
                FROM meal_slots s
                WHERE s.id = e.meal_slot_id;
            ");

            migrationBuilder.Sql("ALTER TABLE meal_entries ALTER COLUMN meal_type SET NOT NULL;");

            migrationBuilder.DropColumn(
                name: "meal_slot_id",
                table: "meal_entries");
        }
    }
}
