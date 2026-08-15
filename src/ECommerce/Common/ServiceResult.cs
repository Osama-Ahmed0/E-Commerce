namespace ECommerce.Common
{
    public class ServiceResult<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public string? ErrorMessage { get; init; }
        public ServiceErrorType ErrorType { get; init; }

        public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
        public static ServiceResult<T> Fail(string msg, ServiceErrorType type) =>
            new() { Success = false, ErrorMessage = msg, ErrorType = type };
    }
}
