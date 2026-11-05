using Bunit;
using Encurtador.Web.Models;
using Encurtador.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Encurtador.Web.UnitTests.Infrastructure;

public sealed class FakeShortLinkApi : IShortLinkApi
{
    public Func<CreateLinkRequest, Task<ApiResult<CreatedLinkDto>>> CreateHandler { get; set; } =
        request => Task.FromResult<ApiResult<CreatedLinkDto>>(new ApiResult<CreatedLinkDto>.Success(
            new CreatedLinkDto(
                request.Code ?? "abc1234",
                $"http://localhost/{request.Code ?? "abc1234"}",
                request.DestinationUrl,
                DateTimeOffset.UtcNow,
                request.ExpiresAt)));

    public Func<string, Task<ApiResult<LinkStatsDto>>> StatsHandler { get; set; } =
        code => Task.FromResult<ApiResult<LinkStatsDto>>(new ApiResult<LinkStatsDto>.Success(new LinkStatsDto(code, 0)));

    public List<CreateLinkRequest> CreateCalls { get; } = [];

    public List<string> StatsCalls { get; } = [];

    public Task<ApiResult<CreatedLinkDto>> CreateLinkAsync(CreateLinkRequest request, CancellationToken ct = default)
    {
        CreateCalls.Add(request);
        return CreateHandler(request);
    }

    public Task<ApiResult<LinkStatsDto>> GetStatsAsync(string code, CancellationToken ct = default)
    {
        StatsCalls.Add(code);
        return StatsHandler(code);
    }
}

public sealed class FakeClipboardService : IClipboardService
{
    public bool Succeeds { get; set; } = true;

    public List<string> Copied { get; } = [];

    public ValueTask<bool> CopyAsync(string text)
    {
        Copied.Add(text);
        return ValueTask.FromResult(Succeeds);
    }
}

public sealed class FakeBrowserTimeZone : IBrowserTimeZone
{
    public TimeSpan Offset { get; set; } = TimeSpan.FromHours(-3);

    public ValueTask<TimeSpan> GetOffsetAsync(DateTime localDateTime) => ValueTask.FromResult(Offset);

    public ValueTask<TimeSpan> GetOffsetAtAsync(DateTimeOffset instant) => ValueTask.FromResult(Offset);
}

// xUnit 2 chama Dispose() síncrono, que o MudBlazor (só IAsyncDisposable) não suporta; por isso o contexto é encapsulado.
public abstract class BunitTestContext : IAsyncLifetime
{
    private readonly BunitContext _context = new();

    protected BunitTestContext()
    {
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.Services.AddMudServices();
        _context.Services.AddSingleton<IShortLinkApi>(Api);
        _context.Services.AddSingleton<IClipboardService>(Clipboard);
        _context.Services.AddSingleton<IBrowserTimeZone>(TimeZone);
        _context.Render<MudPopoverProvider>();
    }

    protected FakeShortLinkApi Api { get; } = new();

    protected FakeClipboardService Clipboard { get; } = new();

    protected FakeBrowserTimeZone TimeZone { get; } = new();

    protected IServiceProvider Services => _context.Services;

    protected IRenderedComponent<TComponent> Render<TComponent>(
        Action<ComponentParameterCollectionBuilder<TComponent>>? parameters = null)
        where TComponent : Microsoft.AspNetCore.Components.IComponent =>
        _context.Render(parameters);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();
}
