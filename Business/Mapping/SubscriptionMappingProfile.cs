using AutoMapper;
using LaserCuttingSystem.Domain.DTOs.Subscription;
using LaserCuttingSystem.Domain.Entities.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Mapping
{
    public class SubscriptionMappingProfile : Profile
    {
        public SubscriptionMappingProfile()
        {
            CreateMap<SubscriptionPlan, SubscriptionPlanDto>()
                .ForMember(dest => dest.IsFreePlan, opt => opt.MapFrom(src => src.IsFreePlan))
                .ForMember(dest => dest.HasUnlimitedMachineSelection, opt => opt.MapFrom(src => src.HasUnlimitedMachineSelection))
                .ForMember(dest => dest.MachineSelectionText, opt => opt.MapFrom(src => src.MachineSelectionText));

            CreateMap<UserSubscription, UserSubscriptionDto>()
                .ForMember(dest => dest.Plan, opt => opt.MapFrom(src => src.Plan))
                .ForMember(dest => dest.IsCurrentlyActive, opt => opt.MapFrom(src => src.IsCurrentlyActive))
                .ForMember(dest => dest.DaysRemaining, opt => opt.MapFrom(src => src.DaysRemaining));

            CreateMap<PaymentHistory, PaymentHistoryDto>();

            CreateMap<SubscribeRequest, UserSubscription>()
                // Burada SubscribeRequest -> UserSubscription map edilirken
                // PlanId ve AutoRenew atanacak.
                .ForMember(dest => dest.PlanId, opt => opt.MapFrom(src => src.PlanId))
                .ForMember(dest => dest.AutoRenew, opt => opt.MapFrom(src => src.AutoRenew));
        }
    }
}
