namespace Investments.Infrastructure.Persistence.Configurations;

using Investments.Domain.Fx;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> builder)
    {
        builder.ToTable("fx_rates", t =>
        {
            t.HasCheckConstraint("ck_fx_rates_currency", "currency IN ('EUR', 'USD')");
            t.HasCheckConstraint("ck_fx_rates_mid_positive", "mid > 0");
        });

        // The key doubles as the (currency, effective_date) lookup index and makes inserts idempotent.
        builder.HasKey(x => new { x.Currency, x.EffectiveDate });
        builder.Property(x => x.Currency).HasConversion<string>().HasMaxLength(3).HasColumnName("currency");
        builder.Property(x => x.EffectiveDate).HasColumnName("effective_date");
        builder.Property(x => x.Mid).HasColumnType("numeric(18,6)").HasColumnName("mid");
        builder.Property(x => x.TableNo).IsRequired().HasMaxLength(FxRate.MaxTableNoLength).HasColumnName("table_no");
    }
}
