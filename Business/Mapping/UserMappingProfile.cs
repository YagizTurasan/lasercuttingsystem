using AutoMapper;
using LaserCuttingSystem.Domain.DTOs.User;
using LaserCuttingSystem.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<ApplicationUser, UserDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.ActiveSubscription, opt => opt.MapFrom(src => src.ActiveSubscription))
                .ForMember(dest => dest.SubscriptionLevel, opt => opt.MapFrom(src => src.SubscriptionLevel))
                .ForMember(dest => dest.IsEnterprise, opt => opt.MapFrom(src => src.IsEnterprise))
                .ForMember(dest => dest.SelectedMachineIds, opt => opt.MapFrom(src => src.SelectedMachineIds))
                .ForMember(dest => dest.Roles, opt => opt.Ignore());

            CreateMap<ApplicationUser, UserListDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.CurrentPlan, opt => opt.MapFrom(src =>
                    src.ActiveSubscription != null ? src.ActiveSubscription.Plan.DisplayName : "Ücretsiz"))
                .ForMember(dest => dest.SubscriptionEndDate, opt => opt.MapFrom(src =>
                    src.ActiveSubscription != null ? src.ActiveSubscription.EndDate : (DateTime?)null));

            CreateMap<UpdateProfileRequest, ApplicationUser>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.CompanyName));
        }
    }
}
