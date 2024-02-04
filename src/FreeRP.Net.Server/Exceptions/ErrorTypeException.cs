namespace FreeRP.Net.Server.Exceptions
{
    public class ErrorTypeException : Exception
    {
        public GrpcService.Core.ErrorType ErrorType { get; set; }

        public ErrorTypeException(GrpcService.Core.ErrorType errorType, string message) : base(message) 
        {
            ErrorType = errorType;
        }
    }
}
