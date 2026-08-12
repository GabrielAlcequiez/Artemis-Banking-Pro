namespace ABP.Application.Common.DTOs.Users
{
    public class UserUniquenessResponseDto
    {
        public string? IdentificationError { get; set; }

        public string? EmailError { get; set; }

        public string? UserNameError { get; set; }

        public bool HasError =>
            IdentificationError is not null ||
            EmailError is not null ||
            UserNameError is not null;
    }
}
