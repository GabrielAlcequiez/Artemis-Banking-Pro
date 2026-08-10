using ABP.Application.Features.Accounts.DTOs;
using ABP.Domain.Entities.Accounts;
using AutoMapper;

namespace ABP.Application.Features.Accounts.Mappings
{
    public class AccountsMappingProfile : Profile
    {
        public AccountsMappingProfile()
        {
            CreateMap<SavingsAccount, SavingsAccountSummaryDto>();

           
            CreateMap<SavingsAccount, SavingsAccountDetailDto>()
                .ForMember(dest => dest.RecentTransactions, opt => opt.Ignore());

            CreateMap<AccountTransaction, AccountTransactionDto>();

            
        }
    }
}
