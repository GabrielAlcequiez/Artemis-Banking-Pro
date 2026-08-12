namespace ABP.Domain.ReadModels.Loans;

public sealed record LoanClientCandidateReadModel(
    string Id,
    string Identification,
    string FullName,
    string Email);
