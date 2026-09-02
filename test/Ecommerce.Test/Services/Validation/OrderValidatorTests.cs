using ECommerce.Common;
using ECommerce.Dtos;
using ECommerce.Services.Validation;

namespace Ecommerce.Test.Services.Validation
{
    public class OrderValidatorTests
    {
        [Theory]
        [InlineData("")]
        public async Task ValidateForCreateAsync_emptyShippingAddress_returnsBadRequest(
            string shippingAddress)
        {
            // Arrange
            var validator = new OrderValidator();

            var dto = new CheckoutRequestDto
            {
                ShippingAddress = shippingAddress
            };

            // Act
            var result = await validator.ValidateForCreateAsync(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(
                "Shipping address is required.",
                result.ErrorMessage);
            Assert.Equal(
                ServiceErrorType.BadRequest,
                result.ErrorType);
        }

        [Theory]
        [InlineData("123 Main Street")]
        [InlineData("Cairo, Egypt")]
        [InlineData("10 Ahmed Street, Assiut")]
        public async Task ValidateForCreateAsync_validShippingAddress_returnsValidResult(
            string shippingAddress)
        {
            // Arrange
            var validator = new OrderValidator();

            var dto = new CheckoutRequestDto
            {
                ShippingAddress = shippingAddress
            };

            // Act
            var result = await validator.ValidateForCreateAsync(dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }


        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public async Task ValidateForUpdateStatusAsync_emptyOrWhitespaceStatus_returnsValidationError(
            string orderStatus)
        {
            // Arrange
            var validator = new OrderValidator();

            var dto = new UpdateOrderStatusDto
            {
                OrderStatus = orderStatus
            };

            // Act
            var result = await validator.ValidateForUpdateStatusAsync(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(
                "Status is required.",
                result.ErrorMessage);
            Assert.Equal(
                ServiceErrorType.Validation,
                result.ErrorType);
        }

        [Theory]
        [InlineData("InvalidStatus")]
        [InlineData("Unknown")]
        [InlineData("SomethingElse")]
        public async Task ValidateForUpdateStatusAsync_invalidStatus_returnsValidationError(
            string orderStatus)
        {
            // Arrange
            var validator = new OrderValidator();

            var dto = new UpdateOrderStatusDto
            {
                OrderStatus = orderStatus
            };

            // Act
            var result = await validator.ValidateForUpdateStatusAsync(dto);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(
                "Invalid order status.",
                result.ErrorMessage);
            Assert.Equal(
                ServiceErrorType.Validation,
                result.ErrorType);
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Paid")]
        [InlineData("Shipped")]
        [InlineData("Delivered")]
        [InlineData("Cancelled")]
        public async Task ValidateForUpdateStatusAsync_validStatus_returnsValidResult(
            string orderStatus)
        {
            // Arrange
            var validator = new OrderValidator();

            var dto = new UpdateOrderStatusDto
            {
                OrderStatus = orderStatus
            };

            // Act
            var result = await validator.ValidateForUpdateStatusAsync(dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Theory]
        [InlineData("pending")]
        [InlineData("PENDING")]
        [InlineData("pEnDiNg")]
        public async Task ValidateForUpdateStatusAsync_statusIsCaseInsensitive_returnsValidResult(
            string orderStatus)
        {
            // Arrange
            var validator = new OrderValidator();

            var dto = new UpdateOrderStatusDto
            {
                OrderStatus = orderStatus
            };

            // Act
            var result = await validator.ValidateForUpdateStatusAsync(dto);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }
    }
}