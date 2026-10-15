using AutoMapper;
using LaserCuttingSystem.Domain.DTOs.Machine;
using LaserCuttingSystem.Domain.Entities.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Mapping
{
    public class MachineMappingProfile : Profile
    {
        public MachineMappingProfile()
        {
            CreateMap<LaserMachine, LaserMachineDto>()
                .ForMember(dest => dest.Models, opt => opt.MapFrom(src => src.MachineModels))
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())  // Eğer servis içinde set ediyorsan ignore edebilirsin
                .ForMember(dest => dest.SelectionOrder, opt => opt.Ignore())
                .ForMember(dest => dest.HasAccess, opt => opt.Ignore())
                .ForMember(dest => dest.AccessType, opt => opt.Ignore());

            CreateMap<LaserMachine, LaserMachineListDto>()
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.SelectionOrder, opt => opt.Ignore())
                .ForMember(dest => dest.HasAccess, opt => opt.Ignore())
                .ForMember(dest => dest.AccessType, opt => opt.Ignore());

            CreateMap<MachineModel, MachineModelDto>();

            CreateMap<MachinePart, MachinePartDto>();

            CreateMap<MachineSection, MachineSectionDto>();
        }
    }
}
