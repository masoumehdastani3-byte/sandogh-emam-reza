using Contracts.Enums;
using System;

namespace Contracts.Models
{
    public class NotificationLogDto : MainEntitiesDto
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

        public string NotificationTypeName => EnumDescription.GetEnumDescription((NotificationType)NotificationType);
        public string NotificationStatusName => EnumDescription.GetEnumDescription((NotificationStatus)NotificationStatus);
        public string UserFullName { get; set; }
    }
}
