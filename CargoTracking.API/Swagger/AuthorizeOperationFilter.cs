using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CargoTracking.API.Swagger;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo
           .GetCustomAttributes(true)
           .OfType<AuthorizeAttribute>()
           .Any()
           || context.MethodInfo.DeclaringType?
               .GetCustomAttributes(true)
               .OfType<AuthorizeAttribute>()
               .Any() == true;

        if (!hasAuthorize)
        {
            return;
        }

        operation.Security ??= [];

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });
    }
}
