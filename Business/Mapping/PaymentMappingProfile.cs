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
    public class PaymentMappingProfile : Profile
    {
        public PaymentMappingProfile()
        {
            CreateMap<PaymentHistory, PaymentHistoryDto>()
                .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Subscription.Plan.DisplayName));
        }
    }
}
