using BlueSchoolSystem.Repository;
using BlueSchoolSystem.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BlueSchoolSystem.Filters;
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireFaceTicketAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var uid = ctx.HttpContext.User.GetUserId();
        var tid = ctx.HttpContext.Request.Headers["X-Face-Ticket"].ToString();

        var store = ctx.HttpContext.RequestServices.GetRequiredService<IFaceTicketStore>();
        if (string.IsNullOrWhiteSpace(tid) || !store.Consume(tid, uid))
        {
            ctx.Result = new UnauthorizedObjectResult("Face verification required.");
            return;
        }
        await next();
    }
}
