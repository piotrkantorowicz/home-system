namespace Household.Infrastructure.Persistence.Configurations;

using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("persons");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PersonId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.AuthSubject)
            .HasMaxLength(255)
            .HasColumnName("auth_subject");

        builder.HasIndex(x => x.AuthSubject)
            .IsUnique()
            .HasFilter("auth_subject IS NOT NULL")
            .HasDatabaseName("idx_persons_auth_subject");

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("display_name");

        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(e => e.Value)
                .IsRequired()
                .HasMaxLength(320)
                .HasColumnName("email");

            email.HasIndex(e => e.Value)
                .HasDatabaseName("idx_persons_email");
        });
        builder.Navigation(x => x.Email).IsRequired(false);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(2048)
            .HasColumnName("avatar_url");

        builder.Property(x => x.IsManaged)
            .HasColumnName("is_managed");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
