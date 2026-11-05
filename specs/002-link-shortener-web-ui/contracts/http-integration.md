# Contrato: integração HTTP com a API (feature 001)

Derivado do código em `src/Encurtador.Api` (não há OpenAPI utilizável em produção; o `openapi.yaml` da 001 é mantido à mão e o `MapOpenApi` só existe em Development). Por isso o cliente tipado é escrito à mão, protegido por teste de contrato em `Encurtador.IntegrationTests`.

## POST /api/links

Request (JSON, camelCase):

```json
{ "destinationUrl": "https://exemplo.com/p", "code": "meu-link", "expiresAt": "2026-12-31T23:59:00-03:00" }
```

`code` e `expiresAt` são opcionais (omitidos quando vazios).

| Status | Corpo | Tratamento no cliente |
|---|---|---|
| 201 | `{code, shortUrl, destinationUrl, createdAt, expiresAt}` | `Success` |
| 400 | ProblemDetails, `title: "Solicitação inválida"`, `detail` em português, sem `errors` | `Rejected` (campo via `ProblemFieldMapper`) |
| 5xx / rede / timeout | qualquer | `Unavailable` |

A API devolve apenas o **primeiro** erro, na ordem: URL, expiração, formato do código, unicidade.

## GET /api/links/{code}/stats

| Status | Corpo | Tratamento |
|---|---|---|
| 200 | `{code, accessCount}` | `Success` |
| 404 | ProblemDetails | `NotFound` |
| 5xx / rede | — | `Unavailable` |

Observação: a consulta **não** verifica expiração; link expirado retorna 200 com o total. A spec 002 (US3-2, FR-006) deve ser ajustada para refletir isso.

## Mapeamento de erro por campo

Ver tabela em [../data-model.md](../data-model.md). Correspondência exata do `detail`; qualquer outro texto vira erro geral. Teste de contrato no backend falha se alguma das mensagens mudar.

## GET /{code} (navegador)

Contrato de hospedagem, aditivo:

| Requisição | Resposta |
|---|---|
| Link válido | 302 (inalterado) |
| Inexistente/expirado, `Accept` contém `text/html` | **404 com `index.html` da SPA** (`Content-Type: text/html`) |
| Inexistente/expirado, qualquer outro `Accept` (`application/json`, `*/*`, ausente) | 404 ProblemDetails JSON (inalterado) |
| Banco ocupado | 503 JSON (inalterado, também em navegação) |

A SPA, ao carregar uma URL que não é `/`, casa a rota `/{*path}` e mostra `LinkUnavailable`. Arquivos estáticos (`/`, `_framework/*`, `css/*`, `js/*`, `appsettings*.json`) têm prioridade sobre `/{code}`.

## Configuração

`wwwroot/appsettings.json` → `{ "Api": { "BaseUrl": "" } }`. Vazio = mesma origem (`HostEnvironment.BaseAddress`). Valor preenchido = origem diferente (exigiria CORS na API, fora do plano).
