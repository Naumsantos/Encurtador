using System.Globalization;
using Encurtador.Web;
using Encurtador.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var culture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

var configuredBaseUrl = builder.Configuration["Api:BaseUrl"];
var apiBaseAddress = string.IsNullOrWhiteSpace(configuredBaseUrl)
    ? builder.HostEnvironment.BaseAddress
    : configuredBaseUrl.TrimEnd('/') + "/";

// HttpClient próprio evita o pacote Microsoft.Extensions.Http (AddHttpClient) só para dois endpoints.
builder.Services.AddScoped<IShortLinkApi>(_ => new ShortLinkApiClient(new HttpClient
{
    BaseAddress = new Uri(apiBaseAddress),
    Timeout = ShortLinkApiClient.ApiTimeout
}));
builder.Services.AddScoped<IClipboardService, ClipboardService>();
builder.Services.AddScoped<IBrowserTimeZone, BrowserTimeZone>();

await builder.Build().RunAsync();
