namespace Domain.Repositories
{
    public interface IRepositoryManager
    {
        IInstallmentRepository InstallmentRepository { get; }
        ILoanRepository LoanRepository { get; }
        IPaymentRepository PaymentRepository { get; }
        IUserRepository UserRepository { get; }
        IUserRoleMenuAccessRepository UserRoleMenuAccessRepository { get; }
        IUserRoleRepository UserRoleRepository { get; }
        IUserSubMenuRepository UserSubMenuRepository { get; }
        IUnitOfWork UnitOfWork { get; }
        I_EnumsRepository _EnumsRepository { get; }
        ISettingRepository SettingRepository { get; }
        INotificationLogRepository NotificationLogRepository { get; }
        IFundTransactionRepository FundTransactionRepository { get; }
        ILoginOtpRepository LoginOtpRepository { get; }
    }
}
