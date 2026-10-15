using AutoMapper;
using LaserCuttingSystem.Domain.DTOs.Error;
using LaserCuttingSystem.Domain.Entities.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Mapping
{
    public class ErrorMappingProfile : Profile
    {
        public ErrorMappingProfile()
        {
            CreateMap<ErrorCode, ErrorCodeDto>()
                .ForMember(dest => dest.ErrorSource, opt => opt.MapFrom(src => src.ErrorSource));

            CreateMap<ErrorCode, ErrorCodeListDto>()
                .ForMember(dest => dest.SourceName, opt => opt.MapFrom(src =>
                    src.ErrorSource != null ? src.ErrorSource.SourceName : null));

            CreateMap<ErrorSource, ErrorSourceDto>();
        }
    }
}
