using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    internal sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
    {
        public void Configure(EntityTypeBuilder<NotificationLog> builder)
        {
            builder.ToTable(nameof(NotificationLog));
            builder.HasKey(x => x.NotificationId);
            builder.Property(x => x.NotificationId).ValueGeneratedOnAdd();
            builder.Property(x => x.Channel).HasMaxLength(20).HasDefaultValue("Bale");
            builder.Property(x => x.RelatedEntity).HasMaxLength(50);
            builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
            builder.Property(x => x.CreateDate).HasDefaultValueSql("GETDATE()");
            builder.Property(x => x.Active).HasDefaultValue(true);

            builder.HasOne(d => d.User)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotificationLog_User");
        }
    }
}
