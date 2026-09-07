using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Contracts.Enums
{
    public enum PaymentStatus
    {
        [Description("دریافت")]
        Receipt = 1,
        [Description("پرداخت")]
        Pay = 2
    }

    public enum PaymentType
    {
        [Description("قسط وام")]
        Installment = 1,
        [Description("کمک خیرین")]
        Donation = 2,
        [Description("پرداخت دستی")]
        Manual = 3,
        [Description("پرداخت وام به عضو")]
        LoanDisbursement = 4
    }

    public enum PaymentMethod
    {
        [Description("زرین‌پال")]
        ZarinPal = 1,
        [Description("دستی")]
        Manual = 2
    }

    public enum GatewayStatus
    {
        [Description("در انتظار")]
        Pending = 1,
        [Description("موفق")]
        Success = 2,
        [Description("ناموفق")]
        Failed = 3
    }

    public enum LoanStatus
    {
        [Description("تسویه شده")]
        Cleared = 1,
        [Description("عادی")]
        Normal = 2,
        [Description("عقب افتاده")]
        Delayed = 3,
        [Description("اهدا شده")]
        Donated = 4
    }

    public enum LoanRequestStatus
    {
        [Description("در صف انتظار")]
        WaitingInQueue = 1,
        [Description("تایید و پرداخت‌شده")]
        Disbursed = 2,
        [Description("رد شده")]
        Rejected = 3,
        [Description("لغو شده")]
        Cancelled = 4
    }

    public enum InstallmentStatus
    {
        [Description("تسویه شده")]
        Cleared = LoanStatus.Cleared,
        [Description("عادی")]
        Normal = LoanStatus.Normal,
        [Description("عقب افتاده")]
        Delayed = LoanStatus.Delayed,
        [Description("اهدا شده")]
        Donated = LoanStatus.Donated
    }

    public enum UserActiveType
    {
        [Description("فعال")]
        Active = 1,
        [Description("مسدود")]
        Blocked = 2
    }

    public enum UserStatusLife
    {
        [Description("زنده")]
        living = 1,
        [Description("مرده")]
        Dead = 2
    }

    public enum UserStatus
    {
        [Description("اهدا کننده")]
        Donator = 1,
        [Description("وام گیرنده")]
        Borrower = 2,
        [Description("متقاضی وام")]
        LoanApplicant = 3,
        [Description("ضامن")]
        Sponsor = 4,
        [Description("مدیر صندوق")]
        Manager = 5
    }

    public enum UserGender
    {
        [Description("زن")]
        Famale = 1,
        [Description("مرد")]
        Man = 2
    }

    public enum UserRoleType
    {
        [Description("مدیر صندوق")]
        Manager = 1,
        [Description("عضو / وام‌گیرنده")]
        Member = 2,
        [Description("خیر")]
        Donator = 3
    }

    public enum NotificationType
    {
        [Description("رمز عبور")]
        Password = 1,
        [Description("یادآوری قسط")]
        InstallmentReminder = 2,
        [Description("لینک پرداخت خیرین")]
        DonationLink = 3,
        [Description("اطلاع واریزی به مدیر")]
        AdminPaymentNotice = 4,
        [Description("پرداخت وام")]
        LoanDisbursed = 5,
        [Description("کد ورود")]
        OtpLogin = 6
    }

    public enum NotificationStatus
    {
        [Description("ارسال شده")]
        Sent = 1,
        [Description("ناموفق")]
        Failed = 2,
        [Description("در انتظار")]
        Pending = 3
    }

    public enum FundTransactionType
    {
        [Description("واریز خیرین")]
        DonationIn = 1,
        [Description("دریافت قسط")]
        InstallmentIn = 2,
        [Description("پرداخت وام")]
        LoanOut = 3,
        [Description("تعدیل دستی")]
        ManualAdjust = 4
    }

    public static class EnumDescription
    {
        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            if (attributes != null && attributes.Any())
                return attributes.First().Description;
            return value.ToString();
        }
    }
}
