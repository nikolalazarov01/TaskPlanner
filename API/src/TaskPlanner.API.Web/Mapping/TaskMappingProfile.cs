using AutoMapper;
using TaskPlanner.API.Core.Models.Task;

namespace TaskPlanner.API.Web.Mapping;


public class TaskMappingProfile : Profile
{
    public TaskMappingProfile()
    {
        CreateMap<Data.Models.Task, TaskResponseModel>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId.ToString()))
            .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.CategoryId))
            .ForMember(d => d.Description, o => o.MapFrom(s => s.Description))
            .ForMember(d => d.Deadline, o => o.MapFrom(s => s.Deadline))
            .ForMember(d => d.Priority, o => o.MapFrom(s => s.Priority))
            .ForMember(d => d.EstimatedMinutes, o => o.MapFrom(s => s.EstimatedMinutes))
            .ForMember(d => d.Status, o => o.Ignore());
    }
}