using AutoMapper;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Web.Mapping;

public class CategoryMappingProfile : Profile
{
    public CategoryMappingProfile()
    {
        CreateMap<Category, CategoryResponseModel>()
            .ForMember(d => d.Id,     o => o.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId.ToString()));
    }
}