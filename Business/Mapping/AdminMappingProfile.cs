using AutoMapper;
using LaserCuttingSystem.Business.Extensions;
using LaserCuttingSystem.Domain.DTOs.Admin.AdminDashboard;
using LaserCuttingSystem.Domain.DTOs.Admin.AdminRevenueStats;
using LaserCuttingSystem.Domain.DTOs.Admin.AdminSubscription;
using LaserCuttingSystem.Domain.DTOs.Admin.ErrorCodes;
using LaserCuttingSystem.Domain.DTOs.Admin.Machines;
using LaserCuttingSystem.Domain.DTOs.Admin.Users;
using LaserCuttingSystem.Domain.Entities.Analytics;
using LaserCuttingSystem.Domain.Entities.Error;
using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Domain.Entities.Machine;
using LaserCuttingSystem.Domain.Entities.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Mapping
{
    public class AdminMappingProfile : Profile
    {
        public AdminMappingProfile()
        {
            // Global configuration to ignore unmapped properties
            this.AllowNullCollections = true;
            this.AllowNullDestinationValues = true;

            ConfigureUserMappings();
            ConfigureMachineMappings();
            ConfigureErrorCodeMappings();
            ConfigureSubscriptionMappings();
            ConfigureAnalyticsMappings();
        }

        private void ConfigureUserMappings()
        {
            // ApplicationUser -> AdminUserListDto
            CreateMap<ApplicationUser, AdminUserListDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.CurrentPlan, opt => opt.MapFrom(src =>
                    src.ActiveSubscription != null ? src.ActiveSubscription.Plan!.DisplayName : null))
                .ForMember(dest => dest.SubscriptionEndDate, opt => opt.MapFrom(src =>
                    src.ActiveSubscription != null ? src.ActiveSubscription.EndDate : (DateTime?)null))
                .ForMember(dest => dest.TotalPayments, opt => opt.MapFrom(src =>
                    src.PaymentHistories.Where(p => p.Status == Domain.Enums.PaymentStatus.Completed).Sum(p => p.Amount)))
                .ForMember(dest => dest.SelectedMachinesCount, opt => opt.MapFrom(src =>
                    src.UserMachineSelections.Count(ums => ums.IsActive)))
                .ForMember(dest => dest.Roles, opt => opt.Ignore()) // Will be set manually in service
                .IgnoreUnmapped();

            // ApplicationUser -> AdminUserDetailDto
            CreateMap<ApplicationUser, AdminUserDetailDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.ActiveSubscription, opt => opt.MapFrom(src => src.ActiveSubscription))
                .ForMember(dest => dest.SubscriptionHistory, opt => opt.MapFrom(src => src.UserSubscriptions))
                .ForMember(dest => dest.MachineSelections, opt => opt.MapFrom(src =>
                    src.UserMachineSelections.Where(ums => ums.IsActive)))
                .ForMember(dest => dest.PaymentHistory, opt => opt.MapFrom(src => src.PaymentHistories))
                .ForMember(dest => dest.TotalPayments, opt => opt.MapFrom(src =>
                    src.PaymentHistories.Where(p => p.Status == Domain.Enums.PaymentStatus.Completed).Sum(p => p.Amount)))
                .ForMember(dest => dest.TotalLogins, opt => opt.Ignore()) // Set in service from UserActivity
                .ForMember(dest => dest.LastLoginDate, opt => opt.Ignore()) // Set in service from UserActivity
                .ForMember(dest => dest.Roles, opt => opt.Ignore()) // Will be set manually in service
                .IgnoreUnmapped();

            // UserSubscription -> AdminUserSubscriptionDto
            CreateMap<UserSubscription, AdminUserSubscriptionDto>()
                .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan!.Name))
                .ForMember(dest => dest.PlanDisplayName, opt => opt.MapFrom(src => src.Plan!.DisplayName))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.IsCurrentlyActive, opt => opt.MapFrom(src => src.IsCurrentlyActive))
                .ForMember(dest => dest.DaysRemaining, opt => opt.MapFrom(src => src.DaysRemaining))
                .IgnoreUnmapped();

            // UserMachineSelection -> AdminUserMachineSelectionDto
            CreateMap<UserMachineSelection, AdminUserMachineSelectionDto>()
                .ForMember(dest => dest.MachineId, opt => opt.MapFrom(src => src.MachineId))
                .ForMember(dest => dest.MachineBrand, opt => opt.MapFrom(src => src.Machine!.Brand))
                .ForMember(dest => dest.SelectionOrder, opt => opt.MapFrom(src => src.SelectionOrder))
                .ForMember(dest => dest.SelectedAt, opt => opt.MapFrom(src => src.SelectedAt))
                .IgnoreUnmapped();

            // PaymentHistory -> AdminPaymentHistoryDto
            CreateMap<PaymentHistory, AdminPaymentHistoryDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src =>
                    src.Subscription != null && src.Subscription.Plan != null ?
                    src.Subscription.Plan.DisplayName : "N/A"))
                .IgnoreUnmapped();

            // Request DTOs to Entity mappings
            CreateMap<AdminCreateUserRequest, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => src.EmailConfirmed))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
                .IgnoreUnmapped();

            CreateMap<AdminUpdateUserRequest, ApplicationUser>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .IgnoreUnmapped();
        }

        private void ConfigureMachineMappings()
        {
            // LaserMachine -> AdminMachineListDto
            CreateMap<LaserMachine, AdminMachineListDto>()
                .ForMember(dest => dest.ModelsCount, opt => opt.MapFrom(src => src.MachineModels.Count))
                .ForMember(dest => dest.ErrorCodesCount, opt => opt.MapFrom(src => src.ErrorCodes.Count))
                .ForMember(dest => dest.UsersCount, opt => opt.MapFrom(src =>
                    src.UserMachineSelections.Count(ums => ums.IsActive)))
                .IgnoreUnmapped();

            // Request DTOs to Entity mappings
            CreateMap<AdminMachineCreateRequest, LaserMachine>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .IgnoreUnmapped();

            CreateMap<AdminMachineUpdateRequest, LaserMachine>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .IgnoreUnmapped();

            CreateMap<AdminMachineModelRequest, MachineModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MachineId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .IgnoreUnmapped();

            CreateMap<AdminMachineSectionRequest, MachineSection>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MachineModelId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .IgnoreUnmapped();

            CreateMap<AdminMachinePartRequest, MachinePart>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MachineSectionId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .IgnoreUnmapped();
        }

        private void ConfigureErrorCodeMappings()
        {
            // ErrorCode -> AdminErrorCodeListDto
            CreateMap<ErrorCode, AdminErrorCodeListDto>()
                .ForMember(dest => dest.MachineBrand, opt => opt.MapFrom(src =>
                    src.Machine != null ? src.Machine.Brand :
                    src.MachinePart != null ? src.MachinePart.MachineSection.MachineModel.Machine.Brand : null))
                .ForMember(dest => dest.PartName, opt => opt.MapFrom(src =>
                    src.MachinePart != null ? src.MachinePart.PartName : null))
                .ForMember(dest => dest.SourceName, opt => opt.MapFrom(src =>
                    src.ErrorSource != null ? src.ErrorSource.SourceName : null))
                .ForMember(dest => dest.AccessLevel, opt => opt.MapFrom(src => src.AccessLevel))
                .IgnoreUnmapped();

            // Request DTOs to Entity mappings
            CreateMap<AdminErrorCodeCreateRequest, ErrorCode>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .IgnoreUnmapped();

            CreateMap<AdminErrorCodeUpdateRequest, ErrorCode>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .IgnoreUnmapped();

            CreateMap<AdminErrorSourceRequest, ErrorSource>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .IgnoreUnmapped();
        }

        private void ConfigureSubscriptionMappings()
        {
            // For subscription statistics - Plan distribution mapping
            CreateMap<SubscriptionPlan, PlanDistributionDto>()
                .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.UserCount, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.Revenue, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.Percentage, opt => opt.Ignore()) // Set in service
                .IgnoreUnmapped();

            // Admin subscription request to entity
            CreateMap<AdminUserSubscriptionRequest, UserSubscription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.EndDate, opt => opt.Ignore()) // Calculated in service
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.SubscriptionStatus.Active))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .IgnoreUnmapped();
        }

        private void ConfigureAnalyticsMappings()
        {

            CreateMap<UserActivity, RecentActivityDto>()
                .ForMember(dest => dest.ActivityType, opt => opt.MapFrom(src => src.ActivityType))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.TimeAgo, opt => opt.Ignore()) // Calculated in service
                .IgnoreUnmapped();


            CreateMap<IGrouping<object, PaymentHistory>, MonthlyRevenueDto>()
                .ForMember(dest => dest.Revenue, opt => opt.MapFrom(src => src.Sum(p => p.Amount)))
                .ForMember(dest => dest.TransactionCount, opt => opt.MapFrom(src => src.Count()))
                .ForMember(dest => dest.Year, opt => opt.Ignore()) // Set manually
                .ForMember(dest => dest.Month, opt => opt.Ignore()) // Set manually
                .ForMember(dest => dest.MonthName, opt => opt.Ignore()) // Set manually
                .IgnoreUnmapped();
        }
    }
    
}
