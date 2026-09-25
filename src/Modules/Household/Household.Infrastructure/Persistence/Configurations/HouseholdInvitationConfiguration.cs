namespace Household.Infrastructure.Persistence.Configurations;

using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class HouseholdInvitationConfiguration : IEntityTypeConfiguration<HouseholdInvitation>
{
    public void Configure(EntityTypeBuilder<HouseholdInvitation> builder)
    {
        builder.ToTable("household_invitations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => HouseholdInvitationId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.HouseholdId)
            .HasConversion(id => id.Value, value => HouseholdId.From(value))
            .HasColumnName("household_id");

        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(e => e.Value)
                .IsRequired()
                .HasMaxLength(320)
                .HasColumnName("email");

            email.HasIndex(e => e.Value).HasDatabaseName("idx_household_invitations_email");
        });
        builder.Navigation(x => x.Email).IsRequired(false);

        builder.Property(x => x.TargetPersonId)
            .HasConversion(id => id!.Value, value => PersonId.From(value))
            .HasColumnName("target_person_id");

        builder.HasIndex(x => x.TargetPersonId).HasDatabaseName("idx_household_invitations_target_person");

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasColumnName("role");

        builder.Property(x => x.InvitedByPersonId)
            .HasConversion(id => id.Value, value => PersonId.From(value))
            .HasColumnName("invited_by_person_id");

        builder.Property(x => x.Nickname)
            .HasMaxLength(100)
            .HasColumnName("nickname");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasColumnName("status");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.ResolvedAt).HasColumnName("resolved_at");

        builder.HasIndex(x => x.HouseholdId).HasDatabaseName("idx_household_invitations_household");

        // Maps to PostgreSQL's xmin system column — no schema change, no migration. See
        // HouseholdInvitation.Version and InvitationConflictGuard.
        builder.Property(x => x.Version).IsRowVersion();
    }
}
