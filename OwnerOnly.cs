using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GreenTasty.Preorder.Api;

/// <summary>
/// Owner-only endpoint. Who is calling: <c>X-User-Id</c> header → <c>Authorization: Bearer</c> →
/// <c>?userId=</c> query → the owner by default (so plain links just work in a browser).
/// Unknown id → 401, not an owner → 403.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class OwnerOnlyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var store = http.RequestServices.GetRequiredService<Store>();
        var userId = ResolveUserId(http);

        if (!store.Users.TryGetValue(userId, out var user))
        {
            await WriteError(http, 401, $"Unknown user '{userId}': pass X-User-Id header (e.g. X-User-Id: owner-1)");
            return;
        }

        if (user.Role != Role.Owner)
        {
            await WriteError(http, 403, $"User '{user.Name}' is not an Owner");
            return;
        }

        http.Items["user"] = user;
        await next();
    }

    private static string ResolveUserId(HttpContext http)
    {
        var userId = http.Request.Headers["X-User-Id"].ToString();

        if (string.IsNullOrEmpty(userId))
        {
            var authorization = http.Request.Headers.Authorization.ToString();
            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                userId = authorization["Bearer ".Length..].Trim();
        }

        if (string.IsNullOrEmpty(userId)) userId = http.Request.Query["userId"].ToString();

        return string.IsNullOrEmpty(userId) ? "owner-1" : userId;
    }

    private static async Task WriteError(HttpContext http, int statusCode, string message)
    {
        http.Response.StatusCode = statusCode;
        await http.Response.WriteAsJsonAsync(new ErrorResponse(message));
    }
}

/// <summary>Shows the X-User-Id header as an input field in Swagger UI for owner-only endpoints.</summary>
public class AuthHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttribute<OwnerOnlyAttribute>() is null) return;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-User-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Caller id: owner-1 (Owner, default) or guest-1 (Guest)",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }
}
