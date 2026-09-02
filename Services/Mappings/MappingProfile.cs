using AutoMapper;
using Contracts.Models;
using Domain.Entities;

namespace Services.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.UserRole != null ? src.UserRole.RoleName : null));
            CreateMap<UserRole, UserRoleDto>();
            CreateMap<Loan, LoanDto>();
            CreateMap<Payment, PaymentDto>();
            CreateMap<Installment, InstallmentDto>();
            CreateMap<UserRoleMenuAccess, UserRoleMenuAccessDto>();
            CreateMap<UserMenu, UserMenuDto>();
            CreateMap<UserSubMenu, UserSubMenuDto>();
            CreateMap<Setting, SettingDto>();
            CreateMap<NotificationLog, NotificationLogDto>();
            CreateMap<FundTransaction, FundTransactionDto>();
        }
    }
}
