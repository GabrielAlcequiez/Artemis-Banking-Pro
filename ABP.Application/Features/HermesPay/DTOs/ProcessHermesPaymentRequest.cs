namespace ABP.Application.Features.HermesPay.DTOs
{
    public sealed class ProcessHermesPaymentRequest
    {
        public ProcessHermesPaymentRequest(
            Guid requestedCommerceId,
            string cardNumber,
            int expirationMonth,
            int expirationYear,
            string cvc,
            decimal transactionAmount,
            Guid operationId)
        {
            RequestedCommerceId = requestedCommerceId;
            CardNumber = cardNumber ?? string.Empty;
            ExpirationMonth = expirationMonth;
            ExpirationYear = expirationYear;
            Cvc = cvc ?? string.Empty;
            TransactionAmount = transactionAmount;
            OperationId = operationId;
        }

        public Guid RequestedCommerceId { get; }
        public string CardNumber { get; }
        public int ExpirationMonth { get; }
        public int ExpirationYear { get; }
        public string Cvc { get; }
        public decimal TransactionAmount { get; }
        public Guid OperationId { get; }

        public override string ToString()
        {
            var last4 = CardNumber.Length >= 4 ? CardNumber[^4..] : "****";
            return $"ProcessHermesPaymentRequest {{ RequestedCommerceId = {RequestedCommerceId}, CardNumber = ****{last4}, ExpirationMonth = {ExpirationMonth}, ExpirationYear = {ExpirationYear}, TransactionAmount = {TransactionAmount}, OperationId = {OperationId} }}";
        }
    }
}
