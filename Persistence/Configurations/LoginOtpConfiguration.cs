using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    internal sealed class LoginOtpConfiguration : IEntityTypeConfiguration<LoginOtp>
    {
        public void Configure(EntityTypeBuilder<LoginOtp> builder)
        {
            builder.ToTable(nameof(LoginOtp));
            builder.HasKey(x => x.LoginOtpId);
            builder.Property(x => x.LoginOtpId).ValueGeneratedOnAdd();
            builder.Property(x => x.NationalCode).HasMaxLength(10).IsRequired();
            builder.Property(x => x.OtpHash).HasMaxLength(128).IsRequired();
            builder.Property(x => x.LoginTokenHash).HasMaxLength(128);
            builder.Property(x => x.CreateDate).HasDefaultValueSql("GETDATE()");

            builder.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_LoginOtp_User");
        }
    }
}
