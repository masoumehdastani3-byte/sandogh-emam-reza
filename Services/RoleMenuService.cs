using AutoMapper;
using Contracts.Models;
using Domain.Entities;
using Domain.Repositories;
using Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public sealed class RoleMenuService : IRoleMenuService
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly IAdminService _adminService;
        private readonly IMapper _mapper;

        public RoleMenuService(IRepositoryManager repositoryManager, IAdminService adminService, IMapper mapper)
        {
            _repositoryManager = repositoryManager;
            _adminService = adminService;
            _mapper = mapper;
        }

        public async Task<List<RoleWithMenusDto>> GetRolesWithMenusAsync(CancellationToken cancellationToken = default)
        {
            var roles = (await _repositoryManager.UserRoleRepository.GetAllAsync(cancellationToken)).ToList();
            var users = await _repositoryManager.UserRepository.GetAllAsync(cancellationToken);
            var result = new List<RoleWithMenusDto>();

            foreach (var role in roles)
            {
                var access = (await _repositoryManager.UserRoleMenuAccessRepository
                    .GetByUserRoleIdAsync(role.RoleId, cancellationToken)).ToList();

                result.Add(new RoleWithMenusDto
                {
                    RoleId = role.RoleId,
                    RoleName = role.RoleName,
                    Description = role.Description,
                    Active = role.Active,
                    MenuIds = access.Select(a => a.UserMenuId).Distinct().ToList(),
                    UserCount = users.Count(u => u.RoleId == role.RoleId)
                });
            }

            return result.OrderBy(r => r.RoleId).ToList();
        }

        public async Task<List<UserSubMenuDto>> GetMenusAsync(CancellationToken cancellationToken = default)
        {
            var menus = await _repositoryManager.UserSubMenuRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<List<UserSubMenuDto>>(menus);
        }

        public async Task<List<UserSubMenuDto>> GetMenusForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _repositoryManager.UserRepository.GetByUserIdAsync(userId, cancellationToken);
            if (user == null) return new List<UserSubMenuDto>();

            var access = (await _repositoryManager.UserRoleMenuAccessRepository
                .GetByUserRoleIdAsync(user.RoleId, cancellationToken)).ToList();

            if (!access.Any())
            {
                var all = await _repositoryManager.UserSubMenuRepository.GetAllAsync(cancellationToken);
                return _mapper.Map<List<UserSubMenuDto>>(all.Where(m => m.IsShowMenu != false));
            }

            return access
                .Where(a => a.UserMenu != null && a.UserMenu.IsShowMenu != false)
                .Select(a => _mapper.Map<UserSubMenuDto>(a.UserMenu))
                .OrderBy(m => m.MenuTitle)
                .ToList();
        }

        public async Task<ApiResultDto> CreateRoleAsync(SaveRoleDto dto, CancellationToken cancellationToken = default)
        {
            if (!await _adminService.IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return ApiResultDto.Fail("فقط مدیر صندوق مجاز است");

            if (string.IsNullOrWhiteSpace(dto.RoleName))
                return ApiResultDto.Fail("نام نقش الزامی است");

            var roles = (await _repositoryManager.UserRoleRepository.GetAllAsync(cancellationToken)).ToList();
            if (roles.Any(r => r.RoleName == dto.RoleName.Trim()))
                return ApiResultDto.Fail("این نام نقش قبلاً ثبت شده است");

            var role = new UserRole
            {
                RoleName = dto.RoleName.Trim(),
                Description = dto.Description,
                Active = true,
                CreateDate = DateTime.Now
            };
            _repositoryManager.UserRoleRepository.Insert(role);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return ApiResultDto.Ok("نقش ایجاد شد", new { role.RoleId, role.RoleName });
        }

        public async Task<ApiResultDto> UpdateRoleAsync(int roleId, UpdateRoleDto dto, CancellationToken cancellationToken = default)
        {
            if (!await _adminService.IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return ApiResultDto.Fail("فقط مدیر صندوق مجاز است");

            var role = await _repositoryManager.UserRoleRepository.GetByUserRoleIdAsync(roleId, cancellationToken);
            if (role == null) return ApiResultDto.Fail("نقش یافت نشد");

            if (!string.IsNullOrWhiteSpace(dto.RoleName))
                role.RoleName = dto.RoleName.Trim();
            if (dto.Description != null) role.Description = dto.Description;
            if (dto.Active.HasValue) role.Active = dto.Active;
            role.UpdateDate = DateTime.Now;

            _repositoryManager.UserRoleRepository.Update(role);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return ApiResultDto.Ok("نقش به‌روزرسانی شد");
        }

        public async Task<ApiResultDto> DeleteRoleAsync(int roleId, int managerUserId, CancellationToken cancellationToken = default)
        {
            if (!await _adminService.IsManagerAsync(managerUserId, cancellationToken))
                return ApiResultDto.Fail("فقط مدیر صندوق مجاز است");

            var role = await _repositoryManager.UserRoleRepository.GetByUserRoleIdAsync(roleId, cancellationToken);
            if (role == null) return ApiResultDto.Fail("نقش یافت نشد");

            var users = await _repositoryManager.UserRepository.GetAllAsync(cancellationToken);
            if (users.Any(u => u.RoleId == roleId))
                return ApiResultDto.Fail("این نقش به کاربران اختصاص دارد و قابل حذف نیست");

            var access = (await _repositoryManager.UserRoleMenuAccessRepository
                .GetByUserRoleIdAsync(roleId, cancellationToken)).ToList();
            if (access.Any())
                _repositoryManager.UserRoleMenuAccessRepository.DeleteRange(access);

            _repositoryManager.UserRoleRepository.Delete(role);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return ApiResultDto.Ok("نقش حذف شد");
        }

        public async Task<ApiResultDto> SaveRoleMenuAccessAsync(SaveRoleMenuAccessDto dto, CancellationToken cancellationToken = default)
        {
            if (!await _adminService.IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return ApiResultDto.Fail("فقط مدیر صندوق مجاز است");

            var role = await _repositoryManager.UserRoleRepository.GetByUserRoleIdAsync(dto.RoleId, cancellationToken);
            if (role == null) return ApiResultDto.Fail("نقش یافت نشد");

            var existing = (await _repositoryManager.UserRoleMenuAccessRepository
                .GetByUserRoleIdAsync(dto.RoleId, cancellationToken)).ToList();
            if (existing.Any())
                _repositoryManager.UserRoleMenuAccessRepository.DeleteRange(existing);

            var menuIds = (dto.MenuIds ?? new List<int>()).Distinct().Where(id => id > 0).ToList();
            foreach (var menuId in menuIds)
            {
                _repositoryManager.UserRoleMenuAccessRepository.Insert(new UserRoleMenuAccess
                {
                    RoleId = dto.RoleId,
                    UserMenuId = menuId,
                    Active = true,
                    CreateDate = DateTime.Now
                });
            }

            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return ApiResultDto.Ok("دسترسی منوها ذخیره شد", new { dto.RoleId, menuIds });
        }

        public async Task<ApiResultDto> CreateMenuAsync(SaveMenuDto dto, CancellationToken cancellationToken = default)
        {
            if (!await _adminService.IsManagerAsync(dto.ManagerUserId, cancellationToken))
                return ApiResultDto.Fail("فقط مدیر صندوق مجاز است");

            if (string.IsNullOrWhiteSpace(dto.MenuTitle) || string.IsNullOrWhiteSpace(dto.SelectorId))
                return ApiResultDto.Fail("عنوان و آدرس منو الزامی است");

            var menu = new UserSubMenu
            {
                MenuTitle = dto.MenuTitle.Trim(),
                SelectorID = dto.SelectorId.Trim(),
                ControlID = string.IsNullOrWhiteSpace(dto.ControlId) ? dto.SelectorId.Trim() : dto.ControlId.Trim(),
                Lang = "fa",
                IsShowMenu = true,
                Description = dto.Description,
                Active = true,
                CreateDate = DateTime.Now
            };
            _repositoryManager.UserSubMenuRepository.Insert(menu);
            await _repositoryManager.UnitOfWork.SaveChangesAsync(cancellationToken);
            return ApiResultDto.Ok("منو ایجاد شد", new { menu.UserSubMenuId, menu.MenuTitle });
        }
    }
}
