using ECommerce.Common;
using ECommerce.Dtos;

namespace ECommerce.Services
{
    public interface IPaymentService
    {
        Task<ServiceResult<PaymentIntentResponseDto>> CreatePaymentIntentAsync(int orderId);
        Task<ServiceResult<bool>> HandleWebhookEventAsync(string json, string signatureHeader);
    }
}
