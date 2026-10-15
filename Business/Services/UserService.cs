using AutoMapper;
using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.User;
using LaserCuttingSystem.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UserService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.IsActive)
            {
                return null;
            }

            return await MapToUserDto(user);
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            return await MapToUserDto(user);
        }

        public async Task<PagedResult<UserListDto>> GetUsersAsync(SearchRequest request)
        {
            var query = _userManager.Users.AsQueryable();

            // Aktif kullanıcıları filtrele
            query = query.Where(u => u.IsActive);

            // Arama filtresi
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var searchTerm = request.Query.ToLowerInvariant();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    (u.CompanyName != null && u.CompanyName.ToLower().Contains(searchTerm)));
            }

            // Ek filtreler
            if (request.Filters != null && request.Filters.Any())
            {
                // Company filtresi
                if (request.Filters.TryGetValue("hasCompany", out var hasCompany))
                {
                    if (bool.TryParse(hasCompany?.ToString(), out var hasCompanyBool))
                    {
                        if (hasCompanyBool)
                        {
                            query = query.Where(u => !string.IsNullOrEmpty(u.CompanyName));
                        }
                        else
                        {
                            query = query.Where(u => string.IsNullOrEmpty(u.CompanyName));
                        }
                    }
                }

                // Kayıt tarihi filtresi
                if (request.Filters.TryGetValue("createdAfter", out var createdAfter))
                {
                    if (DateTime.TryParse(createdAfter?.ToString(), out var afterDate))
                    {
                        query = query.Where(u => u.CreatedAt >= afterDate);
                    }
                }

                if (request.Filters.TryGetValue("createdBefore", out var createdBefore))
                {
                    if (DateTime.TryParse(createdBefore?.ToString(), out var beforeDate))
                    {
                        query = query.Where(u => u.CreatedAt <= beforeDate);
                    }
                }
            }

            // Sıralama
            query = request.SortBy?.ToLowerInvariant() switch
            {
                "email" => request.SortDescending
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email),
                "firstname" => request.SortDescending
                    ? query.OrderByDescending(u => u.FirstName)
                    : query.OrderBy(u => u.FirstName),
                "lastname" => request.SortDescending
                    ? query.OrderByDescending(u => u.LastName)
                    : query.OrderBy(u => u.LastName),
                "company" => request.SortDescending
                    ? query.OrderByDescending(u => u.CompanyName)
                    : query.OrderBy(u => u.CompanyName),
                "createdat" => request.SortDescending
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            // Toplam sayı
            var totalCount = await query.CountAsync();

            // Sayfalama
            var users = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // DTO mapping
            var userDtos = new List<UserListDto>();
            foreach (var user in users)
            {
                var dto = await MapToUserListDto(user);
                userDtos.Add(dto);
            }

            return new PagedResult<UserListDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                HasPreviousPage = request.PageNumber > 1,
                HasNextPage = request.PageNumber < (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null || !user.IsActive)
                {
                    return false;
                }

                // Update user properties
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.CompanyName = request.CompanyName;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeactivateUserAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return false;
                }

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActivateUserAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return false;
                }

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetUserRolesAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new List<string>();
            }

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task<bool> AssignRoleAsync(Guid userId, string role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return false;
                }

                // Rol var mı kontrol et
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    return false;
                }

                // Kullanıcının zaten bu rolü var mı kontrol et
                if (await _userManager.IsInRoleAsync(user, role))
                {
                    return true; // Zaten var, başarılı sayalım
                }

                var result = await _userManager.AddToRoleAsync(user, role);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveRoleAsync(Guid userId, string role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return false;
                }

                // Kullanıcının bu rolü var mı kontrol et
                if (!await _userManager.IsInRoleAsync(user, role))
                {
                    return true; // Zaten yok, başarılı sayalım
                }

                var result = await _userManager.RemoveFromRoleAsync(user, role);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        // Private helper methods
        private async Task<UserDto> MapToUserDto(ApplicationUser user)
        {
            // Aktif subscription bilgisini al
            var activeSubscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(user.Id);

            // Seçili makine ID'lerini al
            var selectedMachineIds = new List<Guid>();
            if (activeSubscription != null && !activeSubscription.Plan.CanAccessAllMachines)
            {
                var selections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(user.Id);
                selectedMachineIds = selections.Select(s => s.MachineId).ToList();
            }

            // Rolleri al
            var roles = await _userManager.GetRolesAsync(user);

            // Base DTO mapping
            var dto = _mapper.Map<UserDto>(user);

            // Subscription bilgilerini zenginleştir
            if (activeSubscription != null)
            {
                dto = dto with
                {
                    ActiveSubscription = _mapper.Map<Domain.DTOs.Subscription.UserSubscriptionDto>(activeSubscription),
                    SubscriptionLevel = activeSubscription.Plan.Level,
                    IsEnterprise = activeSubscription.Plan.CanAccessAllMachines,
                    CanSelectMachines = !activeSubscription.Plan.CanAccessAllMachines && activeSubscription.Plan.MaxMachineSelection > 0
                };
            }

            // Diğer bilgileri ekle
            dto = dto with
            {
                Roles = roles.ToList(),
                SelectedMachineIds = selectedMachineIds
            };

            return dto;
        }

        private async Task<UserListDto> MapToUserListDto(ApplicationUser user)
        {
            // Base mapping
            var dto = _mapper.Map<UserListDto>(user);

            // Aktif subscription bilgisini al
            var activeSubscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(user.Id);

            if (activeSubscription != null)
            {
                dto = dto with
                {
                    CurrentPlan = activeSubscription.Plan.DisplayName,
                    SubscriptionEndDate = activeSubscription.EndDate
                };
            }

            return dto;
        }
    }
}