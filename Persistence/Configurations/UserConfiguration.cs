using System;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    internal sealed class UserBroConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable(nameof(User));
            builder.HasKey(user => user.UserId);
            builder.Property(user => user.UserId).ValueGeneratedOnAdd();
            builder.Property(user => user.FirstName).HasMaxLength(50);
            builder.Property(user => user.LastName).HasMaxLength(50);
            builder.Property(user => user.FullName).HasMaxLength(100).IsRequired();
            builder.Property(user => user.NationalCode).HasMaxLength(10);
            builder.HasIndex(user => user.NationalCode).IsUnique().HasFilter("[NationalCode] IS NOT NULL");
            builder.Property(user => user.UserName).HasMaxLength(20);
            builder.Property(user => user.Password).HasMaxLength(100);
            builder.Property(user => user.Email).HasMaxLength(200);
            builder.Property(user => user.Mobile).HasMaxLength(11).IsRequired();
            builder.HasIndex(user => user.Mobile).IsUnique();
            builder.Property(user => user.PostalCode).HasMaxLength(10);
            builder.Property(user => user.Address).HasMaxLength(500);
            builder.Property(user => user.BirthDate_Shamsi).HasMaxLength(10);
            builder.Property(user => user.CardNumber).HasMaxLength(20);
            builder.Property(user => user.BankName).HasMaxLength(50);
            builder.Property(user => user.NationalIdCardPath).HasMaxLength(500);
            builder.Property(user => user.NationalIdVerifyMessage).HasMaxLength(500);
            builder.Property(user => user.BaleChatId).HasMaxLength(50);
            builder.Property(user => user.HasBaleApp).HasDefaultValue(true);
            builder.Property(user => user.CreateDate).HasDefaultValueSql("GETDATE()");
            builder.Property(user => user.Active).HasDefaultValue(true);

            builder.HasOne(d => d.UserRole)
                .WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_UserRole_User");
        }
    }
}
