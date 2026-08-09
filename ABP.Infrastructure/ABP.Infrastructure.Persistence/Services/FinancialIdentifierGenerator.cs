using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Services
{
    public sealed class FinancialIdentifierGenerator : IFinancialIdentifierGenerator
    {
        private static readonly Random Random = new();
        private readonly AppDbContext _context;

        public FinancialIdentifierGenerator(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateNineDigitIdentifierAsync(FinancialIdentifierType type, CancellationToken cancellationToken = default)
        {
            for (var attempt = 0; attempt < 25; attempt++)
            {
                var candidate = Random.Next(0, 1_000_000_000).ToString("D9");

                var exists = await _context.FinancialIdentifiers
                    .AsNoTracking()
                    .AnyAsync(x => x.Value == candidate, cancellationToken);
                if (exists)
                {
                    continue;
                }

                var identifier = new FinancialIdentifier(Guid.NewGuid())
                {
                    Value = candidate,
                    Type = type,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };

                _context.FinancialIdentifiers.Add(identifier);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    return candidate;
                }
                catch (DbUpdateException)
                {
                    _context.Entry(identifier).State = EntityState.Detached;
                }
            }

            throw new InvalidOperationException("No se pudo generar un identificador financiero único de 9 dígitos.");
        }
    }
}
