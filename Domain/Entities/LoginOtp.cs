using System;

namespace Domain.Entities
{
    public class LoginOtp
    {
        public int LoginOtpId { get; set; }
        public int UserId { get; set; }
        public string NationalCode { get; set; }
        public string OtpHash { get; set; }
        public string LoginTokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int AttemptCount { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreateDate { get; set; }

        public virtual User User { get; set; }
    }
}
