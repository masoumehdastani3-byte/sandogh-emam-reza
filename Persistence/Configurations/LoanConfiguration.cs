using System;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    internal sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            builder.ToTable(nameof(Loan));
            builder.HasKey(loan => loan.LoanId);
            builder.Property(loan => loan.LoanId).ValueGeneratedOnAdd();
            builder.Property(loan => loan.PaymentDate_Shamsi).HasMaxLength(11);
            builder.Property(loan => loan.RequestDate_Shamsi).HasMaxLength(11);
            builder.Property(loan => loan.LoanAmount).IsRequired();
            builder.Property(loan => loan.LoanStatus).IsRequired();
            builder.Property(loan => loan.LoanRequestStatus).IsRequired();
            builder.Property(loan => loan.RequestDate).IsRequired();
            builder.Property(loan => loan.RejectReason).HasMaxLength(500);
            builder.Property(loan => loan.CreateDate).HasDefaultValueSql("GETDATE()");
            builder.Property(loan => loan.Active).HasDefaultValue(true);

            builder.HasOne(d => d.User)
                .WithMany(p => p.Loans)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Loan_User");

            builder.HasOne(d => d.Payment)
                .WithMany(p => p.Loans)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Loan_Payment");
        }
    }
}
