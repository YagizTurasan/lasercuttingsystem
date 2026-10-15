using LaserCuttingSystem.Business.IServices.IAdminServices;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.DTOs.Admin.Users;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Domain.Entities.Subscription;
using LaserCuttingSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services.AdminServices
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public AdminUserService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<PagedResult<AdminUserListDto>> GetUsersAsync(SearchRequest request)
        {
            var query = _userManager.Users.AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(request.Query))
            {
                query = query.Where(u => u.Email.Contains(request.Query)
                                        || u.FirstName.Contains(request.Query)
                                        || u.LastName.Contains(request.Query)
                                        || (u.CompanyName != null && u.CompanyName.Contains(request.Query)));
            }

            // Apply filters
            foreach (var filter in request.Filters)
            {
                switch (filter.Key.ToLower())
                {
                    case "isactive":
                        if (bool.TryParse(filter.Value.ToString(), out bool isActive))
                        {
                            query = query.Where(u => u.IsActive == isActive);
                        }
                        break;
                    case "emailconfirmed":
                        if (bool.TryParse(filter.Value.ToString(), out bool emailConfirmed))
                        {
                            query = query.Where(u => u.EmailConfirmed == emailConfirmed);
                        }
                        break;
                }
            }

            // Sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                switch (request.SortBy.ToLower())
                {
                    case "email":
                        query = request.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email);
                        break;
                    case "fullname":
                        query = request.SortDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName);
                        break;
                    case "createdat":
                        query = request.SortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt);
                        break;
                    default:
                        query = query.OrderByDescending(u => u.CreatedAt);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(u => u.CreatedAt);
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(u => u.UserSubscriptions.Where(s => s.IsActive))
                    .ThenInclude(s => s.Plan)
                .Include(u => u.UserMachineSelections.Where(ms => ms.IsActive))
                .Include(u => u.PaymentHistories.Where(p => p.Status == PaymentStatus.Completed))
                .ToListAsync();

            var userDtos = new List<AdminUserListDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var activeSubscription = user.ActiveSubscription;

                userDtos.Add(new AdminUserListDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName,
                    CompanyName = user.CompanyName,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    CurrentPlan = activeSubscription?.Plan?.DisplayName,
                    SubscriptionEndDate = activeSubscription?.EndDate,
                    TotalPayments = user.PaymentHistories.Sum(p => p.Amount),
                    SelectedMachinesCount = user.UserMachineSelections.Count,
                    Roles = roles.ToList()
                });
            }

            return new PagedResult<AdminUserListDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                HasPreviousPage = request.PageNumber > 1,
                HasNextPage = request.PageNumber < Math.Ceiling((double)totalCount / request.PageSize)
            };
        }

        public async Task<AdminUserDetailDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _userManager.Users
                .Include(u => u.UserSubscriptions.Where(s => s.IsActive))
                    .ThenInclude(s => s.Plan)
                .Include(u => u.UserMachineSelections.Where(ms => ms.IsActive))
                    .ThenInclude(ms => ms.Machine)
                .Include(u => u.PaymentHistories)
                    .ThenInclude(p => p.Subscription)
                        .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            // Get user activities for statistics
            var userActivities = await _unitOfWork.UserActivities
                .FindAsync(a => a.UserId == userId && a.ActivityType == "Login");

            return new AdminUserDetailDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                CompanyName = user.CompanyName,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,

                ActiveSubscription = user.ActiveSubscription != null ? new AdminUserSubscriptionDto
                {
                    Id = user.ActiveSubscription.Id,
                    PlanName = user.ActiveSubscription.Plan!.Name,
                    PlanDisplayName = user.ActiveSubscription.Plan.DisplayName,
                    StartDate = user.ActiveSubscription.StartDate,
                    EndDate = user.ActiveSubscription.EndDate,
                    Status = user.ActiveSubscription.Status.ToString(),
                    AutoRenew = user.ActiveSubscription.AutoRenew,
                    IsCurrentlyActive = user.ActiveSubscription.IsCurrentlyActive,
                    DaysRemaining = user.ActiveSubscription.DaysRemaining
                } : null,

                SubscriptionHistory = user.UserSubscriptions.Select(s => new AdminUserSubscriptionDto
                {
                    Id = s.Id,
                    PlanName = s.Plan!.Name,
                    PlanDisplayName = s.Plan.DisplayName,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Status = s.Status.ToString(),
                    AutoRenew = s.AutoRenew,
                    IsCurrentlyActive = s.IsCurrentlyActive,
                    DaysRemaining = s.DaysRemaining
                }).ToList(),

                MachineSelections = user.UserMachineSelections.Select(ms => new AdminUserMachineSelectionDto
                {
                    Id = ms.Id,
                    MachineId = ms.MachineId,
                    MachineBrand = ms.Machine!.Brand,
                    SelectionOrder = ms.SelectionOrder,
                    SelectedAt = ms.SelectedAt
                }).ToList(),

                PaymentHistory = user.PaymentHistories.Select(p => new AdminPaymentHistoryDto
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    Currency = p.Currency,
                    Status = p.Status.ToString(),
                    TransactionDate = p.TransactionDate,
                    PlanName = p.Subscription?.Plan?.DisplayName ?? "N/A",
                    PaymentProviderId = p.PaymentProviderId
                }).ToList(),

                Roles = roles.ToList(),
                TotalPayments = user.PaymentHistories.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount),
                TotalLogins = userActivities.Count(),
                LastLoginDate = userActivities.OrderByDescending(a => a.CreatedAt).FirstOrDefault()?.CreatedAt
            };
        }

        public async Task<ApiResponse<Guid>> CreateUserAsync(AdminCreateUserRequest request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return ApiResponse<Guid>.Failure("Bu email adresi ile kayıtlı bir kullanıcı zaten var.");
                }

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    CompanyName = request.CompanyName,
                    EmailConfirmed = request.EmailConfirmed,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<Guid>.Failure("Kullanıcı oluşturulamadı.", errors);
                }

                // Assign roles
                foreach (var role in request.Roles)
                {
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }

                // Create initial subscription if specified
                if (request.InitialPlanId.HasValue)
                {
                    var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(request.InitialPlanId.Value);
                    if (plan != null)
                    {
                        var subscription = new UserSubscription
                        {
                            UserId = user.Id,
                            PlanId = plan.Id,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(1), // Default 1 month
                            Status = SubscriptionStatus.Active,
                            AutoRenew = true
                        };

                        await _unitOfWork.UserSubscriptions.AddAsync(subscription);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<Guid>.CreateSuccess(user.Id, "Kullanıcı başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                return ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> UpdateUserAsync(Guid userId, AdminUpdateUserRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return ApiResponse<string>.Failure("Kullanıcı bulunamadı.");
                }

                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.CompanyName = request.CompanyName;
                user.IsActive = request.IsActive;
                user.EmailConfirmed = request.EmailConfirmed;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<string>.Failure("Kullanıcı güncellenemedi.", errors);
                }

                // Update roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                var rolesToRemove = currentRoles.Except(request.Roles);
                var rolesToAdd = request.Roles.Except(currentRoles);

                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

                foreach (var role in rolesToAdd)
                {
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }

                return ApiResponse<string>.CreateSuccess("", "Kullanıcı başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeleteUserAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return ApiResponse<string>.Failure("Kullanıcı bulunamadı.");
                }

                // Soft delete - just deactivate
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<string>.Failure("Kullanıcı silinemedi.", errors);
                }

                // Deactivate subscriptions
                var activeSubscriptions = await _unitOfWork.UserSubscriptions
                    .FindAsync(s => s.UserId == userId && s.IsActive);

                foreach (var subscription in activeSubscriptions)
                {
                    subscription.Cancel();
                }

                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Kullanıcı başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> ActivateUserAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return ApiResponse<string>.Failure("Kullanıcı bulunamadı.");
                }

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<string>.Failure("Kullanıcı aktifleştirilemedi.", errors);
                }

                return ApiResponse<string>.CreateSuccess("", "Kullanıcı başarıyla aktifleştirildi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeactivateUserAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return ApiResponse<string>.Failure("Kullanıcı bulunamadı.");
                }

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<string>.Failure("Kullanıcı pasifleştirilemedi.", errors);
                }

                return ApiResponse<string>.CreateSuccess("", "Kullanıcı başarıyla pasifleştirildi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> AssignRoleAsync(Guid userId, string role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return ApiResponse<string>.Failure("Kullanıcı bulunamadı.");
                }

                if (!await _roleManager.RoleExistsAsync(role))
                {
                    return ApiResponse<string>.Failure("Rol bulunamadı.");
                }

                if (await _userManager.IsInRoleAsync(user, role))
                {
                    return ApiResponse<string>.Failure("Kullanıcı zaten bu role sahip.");
                }

                var result = await _userManager.AddToRoleAsync(user, role);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<string>.Failure("Rol atanamadı.", errors);
                }

                return ApiResponse<string>.CreateSuccess("", "Rol başarıyla atandı.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> RemoveRoleAsync(Guid userId, string role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return ApiResponse<string>.Failure("Kullanıcı bulunamadı.");
                }

                if (!await _userManager.IsInRoleAsync(user, role))
                {
                    return ApiResponse<string>.Failure("Kullanıcı bu role sahip değil.");
                }

                var result = await _userManager.RemoveFromRoleAsync(user, role);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<string>.Failure("Rol kaldırılamadı.", errors);
                }

                return ApiResponse<string>.CreateSuccess("", "Rol başarıyla kaldırıldı.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> ChangeUserSubscriptionAsync(Guid userId, AdminUserSubscriptionRequest request)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return ApiResponse<string>.Failure("Kullanıcı bulunamadı.");
                }

                var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(request.PlanId);
                if (plan == null)
                {
                    return ApiResponse<string>.Failure("Plan bulunamadı.");
                }

                // Deactivate current subscriptions
                var currentSubscriptions = await _unitOfWork.UserSubscriptions
                    .FindAsync(s => s.UserId == userId && s.IsActive);

                foreach (var subscription in currentSubscriptions)
                {
                    subscription.Cancel();
                }

                // Create new subscription
                var newSubscription = new UserSubscription
                {
                    UserId = userId,
                    PlanId = request.PlanId,
                    StartDate = request.StartDate,
                    EndDate = request.StartDate.AddMonths(1), // Default 1 month, can be customized
                    AutoRenew = request.AutoRenew,
                    Status = SubscriptionStatus.Active
                };

                await _unitOfWork.UserSubscriptions.AddAsync(newSubscription);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ApiResponse<string>.CreateSuccess("", "Kullanıcı aboneliği başarıyla değiştirildi.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> ResetPasswordAsync(Guid userId, string newPassword)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return ApiResponse<string>.Failure("Kullanıcı bulunamadı.");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<string>.Failure("Şifre sıfırlanamadı.", errors);
                }

                return ApiResponse<string>.CreateSuccess("", "Şifre başarıyla sıfırlandı.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<List<string>> GetAvailableRolesAsync()
        {
            return await _roleManager.Roles
                .Where(r => r.IsActive)
                .Select(r => r.Name!)
                .ToListAsync();
        }
    }
}
