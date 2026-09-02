using System;

namespace Domain.Entities
{
    public class NotificationLog : MainEntities
    {
        public int NotificationId { get; set; }
        public int? UserId { get; set; }
        public byte NotificationType { get; set; }
        public byte NotificationStatus { get; set; }
        public string Message { get; set; }
        public string Channel { get; set; }
        public DateTime? SentDate { get; set; }
        public string RelatedEntity { get; set; }
        public int? RelatedId { get; set; }
        public string ErrorMessage { get; set; }

        public virtual User User { get; set; }
    }
}
