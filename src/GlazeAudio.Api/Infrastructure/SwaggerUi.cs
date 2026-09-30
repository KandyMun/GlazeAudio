namespace GlazeAudio.Api.Infrastructure;

/// <summary>
/// Minimal Swagger UI page (loaded from a CDN) that renders /openapi/v1.json.
/// Avoids an extra NuGet dependency just to browse the specification.
/// </summary>
public static class SwaggerUi
{
    public const string Html = """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>GlazeAudio API docs</title>
          <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui.css" />
        </head>
        <body>
          <div id="swagger-ui"></div>
          <script src="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
          <script>
            window.ui = SwaggerUIBundle({ url: '/openapi/v1.json', dom_id: '#swagger-ui' });
          </script>
        </body>
        </html>
        """;
}
