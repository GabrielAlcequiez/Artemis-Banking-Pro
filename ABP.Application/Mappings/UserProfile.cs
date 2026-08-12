using System;
using System.Collections.Generic;
using ABP.Application.Common.DTOs.Users;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using AutoMapper;

namespace ABP.Application.Mappings
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // 1. CreateUserDto -> User
            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => Enum.Parse<Roles>(src.Role)))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.SavingsAccounts, opt => opt.Ignore())
                .ForMember(dest => dest.CreditCards, opt => opt.Ignore())
                .ForMember(dest => dest.Loans, opt => opt.Ignore())
                .ForMember(dest => dest.Beneficiaries, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedAtUtc, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedByUserId, opt => opt.Ignore());

            // 2. EditUserDto -> User
            CreateMap<EditUserDto, User>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CommerceId, opt => opt.Ignore())
                .ForMember(dest => dest.SavingsAccounts, opt => opt.Ignore())
                .ForMember(dest => dest.CreditCards, opt => opt.Ignore())
                .ForMember(dest => dest.Loans, opt => opt.Ignore())
                .ForMember(dest => dest.Beneficiaries, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedAtUtc, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedByUserId, opt => opt.Ignore());

            // 3. User -> GetUserDto
            CreateMap<User, GetUserDto>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.CommerceName, opt => opt.Ignore());

            // 3b. User -> UserDetailDto
            CreateMap<User, UserDetailDto>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.MainAccount, opt => opt.Ignore());

            // 4. User -> LoginResponseDto
            CreateMap<User, LoginResponseDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => new List<string> { src.Role.ToString() }))
                .ForMember(dest => dest.HasError, opt => opt.Ignore())
                .ForMember(dest => dest.Error, opt => opt.Ignore());

        }
    }
}
