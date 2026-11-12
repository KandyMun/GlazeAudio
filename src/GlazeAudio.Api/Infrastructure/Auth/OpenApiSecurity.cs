using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GlazeAudio.Api.Infrastructure.Auth;

/// <summary>
/// Documents JWT bearer auth in the OpenAPI spec: a "Bearer" security scheme, attached to every
/// endpoint that requires authorization. Swagger UI then shows an "Authorize" button and lock icons.
/// </summary>
public static class OpenApiSecurity
{
    public static OpenApiOptions AddBearerSecurity(this OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[JwtBearerHandler.SchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Access token from POST /api/auth/login (or /register, /refresh)."
            };
            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, _) =>
        {
            var needsAuth = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any();
            if (needsAuth)
            {
                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(JwtBearerHandler.SchemeName, context.Document)] = []
                });
            }
            return Task.CompletedTask;
        });

        return options;
    }
}
