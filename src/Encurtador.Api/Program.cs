using Encurtador.Api;
using Encurtador.Api.Endpoints;
using Encurtador.Application;
using Encurtador.Application.Links;
using Encurtador.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddLinkInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
builder.Services.AddSingleton<LinkCreationValidator>();
builder.Services.AddScoped<LinkService>();
builder.Services.AddOptions<ShortLinkOptions>()
    .Bind(builder.Configuration.GetSection(ShortLinkOptions.SectionName))
    .Validate(
        options => Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && uri.AbsolutePath == "/"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment),
        "Links:PublicBaseUrl deve ser uma URL raiz absoluta HTTP ou HTTPS sem query ou fragmento.")
    .ValidateOnStart();

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
// Roteamento explícito depois dos arquivos estáticos: sem ele, GET /{code} capturaria /index.html e /appsettings.json.
app.UseRouting();

app.MapLinkEndpoints();
app.Run();

public partial class Program;
