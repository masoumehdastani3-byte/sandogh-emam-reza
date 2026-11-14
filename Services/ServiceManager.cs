using AutoMapper;
using Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Services.Abstractions;
using System;
using System.Net.Http;

namespace Services
{
    public sealed class ServiceManager : IServiceManager
    {
        private readonly Lazy<IUserService> _lazyUserService;
        private readonly Lazy<I_EnumsService> _lazy_EnumsService;
        private readonly Lazy<ISettingService> _lazyISettingService;
        private readonly Lazy<ILoanService> _lazyLoanService;
        private readonly Lazy<IPaymentService> _lazyPaymentService;
        private readonly Lazy<IMemberPanelService> _lazyMemberPanelService;
        private readonly Lazy<IBaleService> _lazyBaleService;
        private readonly Lazy<IBaleMenuService> _lazyBaleMenuService;
        private readonly Lazy<IZarinPalService> _lazyZarinPalService;
        private readonly Lazy<INationalIdVerifyService> _lazyNationalIdVerifyService;
        private readonly Lazy<INotificationSchedulerService> _lazyNotificationSchedulerService;
        private readonly Lazy<IAdminService> _lazyAdminService;
        private readonly Lazy<IRoleMenuService> _lazyRoleMenuService;

        public ServiceManager(
            IRepositoryManager repositoryManager,
            IMapper mapper,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _lazyZarinPalService = new Lazy<IZarinPalService>(() => new ZarinPalService(httpClientFactory, configuration));
            _lazyNationalIdVerifyService = new Lazy<INationalIdVerifyService>(() => new NationalIdVerifyService());
            _lazyBaleService = new Lazy<IBaleService>(() => new BaleService(httpClientFactory, configuration, repositoryManager));
            _lazyUserService = new Lazy<IUserService>(() => new UserService(repositoryManager, mapper, BaleService, NationalIdVerifyService, configuration));
            _lazy_EnumsService = new Lazy<I_EnumsService>(() => new _EnumsService(repositoryManager));
            _lazyISettingService = new Lazy<ISettingService>(() => new SettingService(repositoryManager, mapper));
            _lazyLoanService = new Lazy<ILoanService>(() => new LoanService(repositoryManager, mapper, BaleService));
            _lazyPaymentService = new Lazy<IPaymentService>(() => new PaymentService(
                repositoryManager,
                mapper,
                ZarinPalService,
                BaleService,
                configuration,
                new NikooPaymentHubClient(httpClientFactory, configuration)));
            _lazyMemberPanelService = new Lazy<IMemberPanelService>(() => new MemberPanelService(repositoryManager, mapper));
            _lazyNotificationSchedulerService = new Lazy<INotificationSchedulerService>(() =>
                new NotificationSchedulerService(repositoryManager, BaleService, LoanService, mapper, configuration));
            _lazyAdminService = new Lazy<IAdminService>(() => new AdminService(repositoryManager, mapper));
            _lazyRoleMenuService = new Lazy<IRoleMenuService>(() => new RoleMenuService(repositoryManager, AdminService, mapper));
            _lazyBaleMenuService = new Lazy<IBaleMenuService>(() => new BaleMenuService(
                (BaleService)_lazyBaleService.Value,
                repositoryManager,
                configuration,
                LoanService,
                PaymentService));
        }

        public IUserService UserService => _lazyUserService.Value;
        public I_EnumsService _EnumsService => _lazy_EnumsService.Value;
        public ISettingService SettingService => _lazyISettingService.Value;
        public ILoanService LoanService => _lazyLoanService.Value;
        public IPaymentService PaymentService => _lazyPaymentService.Value;
        public IMemberPanelService MemberPanelService => _lazyMemberPanelService.Value;
        public IBaleService BaleService => _lazyBaleService.Value;
        public IBaleMenuService BaleMenuService => _lazyBaleMenuService.Value;
        public IZarinPalService ZarinPalService => _lazyZarinPalService.Value;
        public INationalIdVerifyService NationalIdVerifyService => _lazyNationalIdVerifyService.Value;
        public INotificationSchedulerService NotificationSchedulerService => _lazyNotificationSchedulerService.Value;
        public IAdminService AdminService => _lazyAdminService.Value;
        public IRoleMenuService RoleMenuService => _lazyRoleMenuService.Value;
    }
}
