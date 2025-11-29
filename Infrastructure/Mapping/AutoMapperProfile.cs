using Application.DTOs.Auth;
using Application.DTOs.Cliente;
using Application.DTOs.Incident;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Application.Helpers;

namespace Infrastructure.Mapping
{
    /// <summary>
    /// AutoMapper profile for mapping between domain entities, DTOs, and persistence entities.
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // User mappings
            CreateMap<User, UserEntity>().ReverseMap();
            CreateMap<User, UserInfoDTO>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.GetFullName()));

            // RefreshToken mappings
            CreateMap<RefreshToken, RefreshTokenEntity>().ReverseMap();

            // Cliente mappings
            CreateMap<Cliente, ClienteEntity>().ReverseMap();
            CreateMap<Cliente, ClienteResponseDTO>();
            CreateMap<CreateClienteDTO, Cliente>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()));
            CreateMap<UpdateClienteDTO, Cliente>();

            // PasswordResetToken mappings
            CreateMap<PasswordResetToken, PasswordResetTokenEntity>().ReverseMap();

            // Incident Domain -> Response DTO
            CreateMap<Incident, IncidentResponseDTO>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.DisplayName))
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Metrics != null ? src.Metrics.CommentCount : 0))
                .ForMember(dest => dest.AttachmentCount, opt => opt.MapFrom(src => src.Metrics != null ? src.Metrics.AttachmentCount : 0))
                .ForMember(dest => dest.PriorityName, opt => opt.MapFrom(src => PriorityHelper.GetDisplayName(src.Priority)))
                .ForMember(dest => dest.PriorityColor, opt => opt.MapFrom(src => PriorityHelper.GetColor(src.Priority)));

            CreateMap<IncidentEntity, IncidentResponseDTO>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.DisplayName))
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Metrics != null ? src.Metrics.CommentCount : 0))
                .ForMember(dest => dest.AttachmentCount, opt => opt.MapFrom(src => src.Metrics != null ? src.Metrics.AttachmentCount : 0))
                .ForMember(dest => dest.PriorityName, opt => opt.MapFrom(src => PriorityHelper.GetDisplayName(src.Priority)))
                .ForMember(dest => dest.PriorityColor, opt => opt.MapFrom(src => PriorityHelper.GetColor(src.Priority)));

            // Create Request DTO -> Incident Domain
            CreateMap<CreateIncidentRequestDTO, Incident>()
                .ForMember(d => d.StatusId, o => o.MapFrom(s => 1)) // OPEN
                .ForMember(d => d.Id, o => o.MapFrom(s => Guid.NewGuid()))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => DateTime.UtcNow));

            // Update Request DTO -> Incident Domain (solo miembros no nulos)
            CreateMap<UpdateIncidentRequestDTO, Incident>()
                .ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

            // IncidentUpdate Domain -> Response DTO
            CreateMap<IncidentUpdate, IncidentUpdateDTO>()
                .ForMember(d => d.AuthorName, o => o.MapFrom(s => s.Author.GetFullName()));

            // Infrastructure Entities -> Domain
            CreateMap<IncidentEntity, Incident>();
            CreateMap<IncidentCategoryEntity, IncidentCategory>();
            CreateMap<IncidentStatusEntity, IncidentStatus>();
            CreateMap<IncidentUpdateEntity, IncidentUpdate>();

            // Domain -> Infrastructure Entities
            CreateMap<Incident, IncidentEntity>();
            CreateMap<IncidentCategory, IncidentCategoryEntity>();
            CreateMap<IncidentStatus, IncidentStatusEntity>();
            CreateMap<IncidentUpdate, IncidentUpdateEntity>();

            // IncidentCategory mappings
            CreateMap<IncidentCategory, IncidentCategoryDTO>();
            CreateMap<IncidentCategoryEntity, IncidentCategoryDTO>();

            // IncidentStatus mappings
            CreateMap<IncidentStatus, IncidentStatusDTO>();
            CreateMap<IncidentStatusEntity, IncidentStatusDTO>();

            // Incident Attachment mappings
            CreateMap<IncidentAttachmentEntity, IncidentAttachment>().ReverseMap();

            // Incident Metric mappings
            CreateMap<IncidentMetricEntity, IncidentMetric>().ReverseMap();
        }
    }
}
