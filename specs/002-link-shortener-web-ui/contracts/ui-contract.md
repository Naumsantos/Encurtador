# Contrato: serviços e interface do cliente

## Serviços injetáveis (`Encurtador.Web/Services`)

```csharp
public interface IShortLinkApi
{
    Task<ApiResult<CreatedLinkDto>> CreateLinkAsync(CreateLinkRequest request, CancellationToken ct = default);
    Task<ApiResult<LinkStatsDto>> GetStatsAsync(string code, CancellationToken ct = default);
}

public interface IBrowserTimeZone
{
    ValueTask<TimeSpan> GetOffsetAsync(DateTime localDateTime);   // via JS getTimezoneOffset
}

public interface IClipboardService
{
    ValueTask<bool> CopyAsync(string text);                      // false se negado/indisponível
}
```

Funções puras e testáveis: `ProblemFieldMapper.Map(detail)`, `ShortCodeParser.Parse(input)`, `ExpirationConverter.Convert(date, time, offset)`.

Regra: componentes `.razor` nunca injetam `HttpClient`; só `IShortLinkApi`, `IClipboardService` e `IBrowserTimeZone`.

## Rotas

| Rota | Componente | Observação |
|---|---|---|
| `/` | `Home` | Criação, resultado e consulta |
| `/{*path}` | `LinkUnavailable` | Qualquer outro caminho (inclui códigos curtos servidos com 404) |

## Elementos de interface (MudBlazor) e seletores estáveis

| Elemento | Componente MudBlazor | `data-testid` |
|---|---|---|
| URL de destino | `MudTextField` | `destination-url` |
| Código personalizado | `MudTextField` | `custom-code` |
| Data / hora de expiração | `MudDatePicker` / `MudTimePicker` | `expiration-date` / `expiration-time` |
| Criar | `MudButton` (com `MudProgressCircular` ao enviar) | `create-submit` |
| Link curto gerado | `MudText`/`MudLink` | `short-url` |
| Copiar | `MudButton` | `copy-button` (rótulo "Copiar" → "Copiado!") |
| Consulta: código ou link | `MudTextField` | `lookup-input` |
| Consultar | `MudButton` | `lookup-submit` |
| Resultado da consulta | `MudAlert` | `lookup-result` |
| Criar novo link (página de erro) | `MudButton` | `new-link-button` |

## Textos principais (pt-BR)

- URL vazia: "Informe a URL de destino."
- Expiração só com hora: "Informe também a data de expiração."
- Consulta vazia: "Informe o código ou o link curto."
- Consulta 404: "Nenhum link encontrado para esse código."
- Indisponível: "Não foi possível falar com o servidor. Tente novamente."
- Cópia: "Copiado!"; falha: "Não foi possível copiar. Selecione o link e copie manualmente."
- Página de erro: "Este link não existe ou expirou." + botão "Criar um novo link".
- Erros de rejeição: o `detail` da API, exibido junto ao campo mapeado.
