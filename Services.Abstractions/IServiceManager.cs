namespace Services.Abstractions
{
    public interface IServiceManager
    {
        IUserService UserService { get; }
        I_EnumsService _EnumsService { get; }
        ISettingService SettingService { get; }
        ILoanService LoanService { get; }
        IPaymentService PaymentService { get; }
        IMemberPanelService MemberPanelService { get; }
        IBaleService BaleService { get; }
        IBaleMenuService BaleMenuService { get; }
        IZarinPalService ZarinPalService { get; }
        INationalIdVerifyService NationalIdVerifyService { get; }
        INotificationSchedulerService NotificationSchedulerService { get; }
        IAdminService AdminService { get; }
        IRoleMenuService RoleMenuService { get; }
    }
}
