namespace TravelPlanner.Api.OpenApi;

/// <summary>
/// Swagger UI подключается с CDN, поэтому NuGet-пакет не нужен:
/// документ формирует встроенный Microsoft.AspNetCore.OpenApi (/openapi/v1.json).
/// </summary>
public static class SwaggerUiPage
{
    public const string Html = """
    <!doctype html>
    <html lang="ru">
    <head>
      <meta charset="utf-8" />
      <title>TravelPlanner API</title>
      <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
      <style>body{margin:0;background:#fafafa}</style>
    </head>
    <body>
      <div id="swagger"></div>
      <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
      <script>
        window.onload = () => SwaggerUIBundle({
          url: '/openapi/v1.json',
          dom_id: '#swagger',
          deepLinking: true,
          persistAuthorization: true
        });
      </script>
    </body>
    </html>
    """;
}