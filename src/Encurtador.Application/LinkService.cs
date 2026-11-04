using Encurtador.Application.Links;
using Encurtador.Application.Persistence;

namespace Encurtador.Application;

public sealed class LinkService
{
    private const int MaximumGeneratedCodeAttempts = 8;
    private readonly ILinkStore _linkStore;
    private readonly IShortCodeGenerator _codeGenerator;
    private readonly TimeProvider _timeProvider;
    private readonly LinkCreationValidator _creationValidator;

    public LinkService(
        ILinkStore linkStore,
        IShortCodeGenerator codeGenerator,
        TimeProvider timeProvider)
        : this(linkStore, codeGenerator, timeProvider, new LinkCreationValidator())
    {
    }

    public LinkService(
        ILinkStore linkStore,
        IShortCodeGenerator codeGenerator,
        TimeProvider timeProvider,
        LinkCreationValidator creationValidator)
    {
        _linkStore = linkStore;
        _codeGenerator = codeGenerator;
        _timeProvider = timeProvider;
        _creationValidator = creationValidator;
    }

    public async Task<CreatedLink> CreateAsync(
        LinkCreationInput input,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var validated = _creationValidator.Validate(input, nowUtc);

        var requestedCode = input.Code;
        if (requestedCode is not null)
        {
            if (await _linkStore.CodeExistsAsync(requestedCode, cancellationToken))
            {
                throw DuplicateCode();
            }

            var requestedLink = CreateEntity(
                requestedCode,
                validated.DestinationUrl,
                nowUtc,
                validated.ExpiresAtUtc);
            if (await _linkStore.CreateAsync(requestedLink, cancellationToken) == LinkCreateStatus.CodeConflict)
            {
                throw DuplicateCode();
            }

            return ToCreatedLink(requestedLink);
        }

        for (var attempt = 0; attempt < MaximumGeneratedCodeAttempts; attempt++)
        {
            var code = _codeGenerator.Generate();
            if (await _linkStore.CodeExistsAsync(code, cancellationToken))
            {
                continue;
            }

            var generatedLink = CreateEntity(
                code,
                validated.DestinationUrl,
                nowUtc,
                validated.ExpiresAtUtc);
            if (await _linkStore.CreateAsync(generatedLink, cancellationToken) == LinkCreateStatus.Created)
            {
                return ToCreatedLink(generatedLink);
            }
        }

        throw new InvalidOperationException("Não foi possível gerar um código único após várias tentativas.");
    }

    public async Task<string?> RedirectAsync(string code, CancellationToken cancellationToken = default)
    {
        var result = await _linkStore.TryRegisterAccessAsync(
            code,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
        return result?.DestinationUrl;
    }

    public Task<LinkStats?> GetStatsAsync(string code, CancellationToken cancellationToken = default)
    {
        return _linkStore.GetStatsAsync(code, cancellationToken);
    }

    private static ShortLink CreateEntity(
        string code,
        string destinationUrl,
        DateTime nowUtc,
        DateTime? expiresAtUtc)
    {
        return new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = code,
            DestinationUrl = destinationUrl,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc,
            AccessCount = 0
        };
    }

    private static CreatedLink ToCreatedLink(ShortLink link)
    {
        return new CreatedLink(link.Code, link.DestinationUrl, link.CreatedAtUtc, link.ExpiresAtUtc);
    }

    private static LinkValidationException DuplicateCode()
    {
        return new LinkValidationException("O código informado já está em uso.");
    }
}
