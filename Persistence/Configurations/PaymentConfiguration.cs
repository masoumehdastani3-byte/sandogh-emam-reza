using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable(nameof(Payment));
            builder.HasKey(payment => payment.PaymentId);
            builder.Property(payment => payment.PaymentId).ValueGeneratedOnAdd();
            builder.Property(payment => payment.RefrenceNumber).HasMaxLength(50);
            builder.Property(payment => payment.CardNumber).HasMaxLength(20);
            builder.Property(payment => payment.ZarinPalAuthority).HasMaxLength(100);
            builder.Property(payment => payment.ZarinPalRefId).HasMaxLength(50);
            builder.Property(payment => payment.FileUload).HasMaxLength(500);
            builder.Property(payment => payment.PaymentDate).IsRequired();
            builder.Property(payment => payment.Amount).IsRequired();
            builder.Property(payment => payment.PaymentStatus).IsRequired();
            builder.Property(payment => payment.PaymentType).IsRequired();
            builder.Property(payment => payment.PaymentMethod).IsRequired();
            builder.Property(payment => payment.GatewayStatus).IsRequired();
            builder.Property(payment => payment.PaymentDate_Shamsi).HasMaxLength(11);
            builder.Property(payment => payment.CreateDate).HasDefaultValueSql("GETDATE()");
            builder.Property(payment => payment.Active).HasDefaultValue(true);

            builder.HasOne(d => d.User)
                .WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Payment_User");
        }
    }
}
