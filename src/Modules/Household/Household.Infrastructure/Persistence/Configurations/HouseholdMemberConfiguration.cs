namespace Household.Infrastructure.Persistence.Configurations;

using Household.Domain.Entities;
using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class HouseholdMemberConfiguration : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(EntityTypeBuilder<HouseholdMember> builder)
    {
        builder.ToTable("household_members");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => HouseholdMemberId.From(value))
            .HasColumnName("id");

        builder.Property<HouseholdId>("HouseholdId")
            .HasConversion(id => id.Value, value => HouseholdId.From(value))
            .HasColumnName("household_id")
            .IsRequired();

        builder.Property(x => x.PersonId)
            .HasConversion(id => id.Value, value => PersonId.From(value))
            .HasColumnName("person_id");

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasColumnName("role");

        builder.Property(x => x.Nickname)
            .HasMaxLength(100)
            .HasColumnName("nickname");

        builder.Property(x => x.JoinedAt).HasColumnName("joined_at");

        builder.HasIndex("HouseholdId", "PersonId")
            .IsUnique()
            .HasDatabaseName("unique_household_person");

        // Non-unique on purpose. "A person belongs to at most one household" is a v1 domain
        // invariant, not a DB constraint — so multi-household can be enabled later without a
        // schema change (see docs/design/household/README.md §4). This index just serves the
        // "which household is this person in" lookup.
        builder.HasIndex(x => x.PersonId)
            .HasDatabaseName("idx_household_members_person");
    }
}
