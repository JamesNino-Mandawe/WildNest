namespace Project.Accomodations
{
    public sealed class EmailVerificationResult
    {
        public bool Success { get; }
        public string Message { get; }

        public EmailVerificationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}
