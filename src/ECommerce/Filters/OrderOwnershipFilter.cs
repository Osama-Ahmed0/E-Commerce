using ECommerce.Data;
using ECommerce.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Filters
{
    public class OrderOwnershipFilter(AppDbContext dbContext) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.GetRole() == "Admin")
            {
                await next();
                return;
            }

            var userId = context.HttpContext.User.GetUserId();
            if (userId == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (context.ActionArguments.TryGetValue("id", out var idObj) && idObj is int orderId)
            {
                var isOwner = await dbContext.Orders.AnyAsync(o => o.Id == orderId && o.UserId == userId);
                if (!isOwner)
                {
                    context.Result = new NotFoundResult();
                    return;
                }
            }

            await next();
        }
    }
}