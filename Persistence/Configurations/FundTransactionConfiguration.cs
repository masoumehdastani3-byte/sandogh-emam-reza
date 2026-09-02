using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    internal sealed class FundTransactionConfiguration : IEntityTypeConfiguration<FundTransaction>
    {
        public void Configure(EntityTypeBuilder<FundTransaction> builder)
        {
            builder.ToTable(nameof(FundTransaction));
            builder.HasKey(x => x.FundTransactionId);
            builder.Property(x => x.FundTransactionId).ValueGeneratedOnAdd();
            builder.Property(x => x.TransactionDate_Shamsi).HasMaxLength(11);
            builder.Property(x => x.Amount).IsRequired();
            builder.Property(x => x.TransactionType).IsRequired();
            builder.Property(x => x.TransactionDate).IsRequired();
            builder.Property(x => x.CreateDate).HasDefaultValueSql("GETDATE()");
            builder.Property(x => x.Active).HasDefaultValue(true);

            builder.HasOne(d => d.User)
                .WithMany(p => p.FundTransactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FundTransaction_User");
        }
    }
}
