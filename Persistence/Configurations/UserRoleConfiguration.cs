using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Persistence.Configurations
{
    internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable(nameof(UserRole));

            builder.HasKey(user => user.RoleId);
            builder.Property(user => user.RoleId).ValueGeneratedOnAdd();
            builder.Property(user => user.RoleName).HasMaxLength(100);
            builder.Property(user => user.CreateDate).HasDefaultValueSql("GETDATE()");
            builder.Property(user => user.Active).HasDefaultValue(true);


        }
    }
}
