# Implementation Plan: Interface web do Encurtador de links

**Branch**: `002-link-shortener-web-ui` | **Date**: 2026-11-04 | **Spec**: [spec.md](./spec.md)

**Input**: Especificação em `specs/002-link-shortener-web-ui/spec.md` e decisões de arquitetura fornecidas para o planejamento.

## Summary

Adicionar `src/Encurtador.Web`, uma aplicação Blazor WebAssembly (.NET 10) com MudBlazor, que consome a API existente sem alterar regras de negócio nem contratos de dados. A API passa a **hospedar** a aplicação (mesma origem do `Links:PublicBaseUrl`), o que viabiliza a página "link inexistente ou expirado": um filtro de endpoint, restrito a `GET /{code}` e a `Accept: text/html`, devolve o `index.html` com status 404; clientes de API continuam recebendo o mesmo ProblemDetails JSON. As chamadas HTTP ficam em um cliente tipado injetável; a URL base vem de `wwwroot/appsettings*.json`. Decisões e alternativas em [research.md](./research.md).

## Technical Context

**Language/Version**: C# / .NET 10 (SDK 10.0.401), Blazor WebAssembly
**Primary Dependencies**: `Microsoft.AspNetCore.Components.WebAssembly` e `MudBlazor` 9.x (Web); `Microsoft.AspNetCore.Components.WebAssembly.Server` (Api, para servir o WASM); `bunit` 2.x e `Microsoft.Playwright` (testes). Justificativas em [research.md](./research.md).
**Storage**: N/A no frontend; backend mantém SQLite (feature 001).
**Testing**: xUnit 2.9.3 (padrão do repositório). Serviços e componentes (bUnit) em `tests/Encurtador.Web.UnitTests`; hospedagem em `tests/Encurtador.IntegrationTests`; ponta a ponta (Playwright) em `tests/Encurtador.E2ETests`.
**Target Platform**: Navegadores atuais de computador e celular (a partir de 320 px); servidor ASP.NET Core .NET 10.
**Project Type**: Aplicação web (frontend WASM hospedado pela API existente).
**Performance Goals**: Resultado de criação em até 1 minuto de interação (SC-001); confirmação de cópia em até 1 s (SC-003). Sem meta de capacidade.
**Constraints**: Sem mudança em regras de negócio ou contratos JSON; `TreatWarningsAsErrors` permanece ativo; textos em português, nomes de classes/arquivos em inglês; sem CORS no caminho principal.
**Scale/Scope**: 2 rotas (`/` e `/{*path}`), 2 formulários, 1 página de link indisponível, 2 endpoints consumidos.

## Constitution Check

| Princípio / restrição | Status | Resposta do plano |
|---|---|---|
| I. Endpoints delegam a serviços de aplicação | PASS | O filtro só altera a apresentação do 404; não acessa EF Core. |
| II. Regras de negócio fora de HTTP | PASS | A UI valida só obrigatoriedade e normaliza entrada (research D5). |
| III. EF Core substituível | PASS | Persistência inalterada. |
| IV. Testes xUnit; integração por endpoint | PASS | Comportamento novo no hospedeiro tem testes de integração; serviços e componentes têm testes unitários. |
| V. Validação = 400 ProblemDetails | PASS | Nenhum contrato de erro muda. |
| .NET 10, C#, Minimal APIs; dependência nova justificada | PASS | Justificativas no research. API segue em Minimal APIs. |
| Nomes em inglês; mensagens em português | PASS | Aplicado ao Web. |
| Commits `tipo(escopo): descrição [REQ-ID]` | PASS | Vale na implementação. |

**Pré-design**: PASS. **Pós-design**: PASS (a única mudança no backend é aditiva, no hospedeiro, sem lógica de negócio).

## Alteração mínima no backend (decisão e impacto)

| Mudança | Onde | Motivo | Impacto |
|---|---|---|---|
| Referência ao `Encurtador.Web` + pacote `...WebAssembly.Server` | `Encurtador.Api.csproj` | Servir a UI na mesma origem do link curto | Build da API compila o Web; publish inclui `wwwroot` |
| `UseDefaultFiles`, `UseBlazorFrameworkFiles`, `UseStaticFiles` | `Program.cs` | Entregar a SPA em `/` e seus ativos | Arquivos estáticos têm prioridade sobre `/{code}` |
| `BrowserNotFoundPageFilter` em `GET /{code}` | `Endpoints/` + `.AddEndpointFilter` | Mostrar a página da spec no navegador | Só afeta 404 com `Accept: text/html`; JSON e `*/*` inalterados |
| `Links:PublicBaseUrl` igual à origem da UI | configuração | `shortUrl` aponta para o host que serve a página | Sem código |

Nenhuma rota, status ou corpo JSON existente muda ([contracts/http-integration.md](./contracts/http-integration.md)).

## Project Structure

### Documentation (this feature)

```text
specs/002-link-shortener-web-ui/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── http-integration.md
│   └── ui-contract.md
├── checklists/requirements.md
└── tasks.md              # gerado por /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── Encurtador.Api/                          # existente; ajustes mínimos
│   ├── Program.cs                           # + arquivos estáticos/WASM
│   ├── Endpoints/
│   │   ├── LinksEndpoints.cs                # + AddEndpointFilter em GET /{code}
│   │   └── BrowserNotFoundPageFilter.cs     # novo
│   └── Encurtador.Api.csproj
├── Encurtador.Application/                  # sem alteração
├── Encurtador.Infrastructure/               # sem alteração
└── Encurtador.Web/                          # novo: Blazor WebAssembly
    ├── Encurtador.Web.csproj
    ├── Program.cs                           # DI, HttpClient tipado, MudBlazor
    ├── App.razor, _Imports.razor
    ├── Layout/MainLayout.razor
    ├── Pages/
    │   ├── Home.razor                       # "/" : criação + consulta
    │   └── LinkUnavailable.razor            # "/{*path}"
    ├── Components/
    │   ├── CreateLinkForm.razor
    │   ├── CreatedLinkCard.razor
    │   └── StatsLookupForm.razor
    ├── Models/
    │   ├── CreateLinkModel.cs, LookupModel.cs
    │   ├── CreatedLinkDto.cs, LinkStatsDto.cs
    ├── Services/
    │   ├── IShortLinkApi.cs, ShortLinkApiClient.cs
    │   ├── ApiResult.cs
    │   ├── ProblemFieldMapper.cs
    │   ├── ShortCodeParser.cs
    │   ├── IBrowserTimeZone.cs, BrowserTimeZone.cs
    │   ├── ExpirationConverter.cs
    │   └── IClipboardService.cs, ClipboardService.cs
    └── wwwroot/
        ├── index.html
        ├── appsettings.json, appsettings.Development.json
        ├── css/app.css
        └── js/interop.js                    # clipboard + fuso

tests/
├── Encurtador.UnitTests/                    # existente, sem alteração
├── Encurtador.IntegrationTests/             # existente; + Hosting/
├── Encurtador.Web.UnitTests/                # novo: serviços + componentes (bUnit)
└── Encurtador.E2ETests/                     # novo: Playwright contra a API real
```

**Structure Decision**: a solution existente permanece intacta; `Encurtador.Web` entra ao lado dos demais em `src/`. Só a API conhece o Web (para hospedá-lo). Os projetos novos são adicionados ao `Encurtador.sln`.

## Integração com a API

Fonte: [contracts/http-integration.md](./contracts/http-integration.md), derivado do código.

- `ShortLinkApiClient` expõe `CreateLinkAsync` e `GetStatsAsync`, devolvendo `ApiResult<T>`: `Success`, `Rejected(field, message)`, `NotFound`, `Unavailable(message)`.
- `Rejected` só para `400`; `ProblemFieldMapper` classifica o `detail` por correspondência exata com as mensagens do backend; sem correspondência, vira mensagem geral.
- Rede, timeout ou `5xx` → `Unavailable`, com campos preservados e nova tentativa.
- URL base: `Api:BaseUrl`; vazio usa `HostEnvironment.BaseAddress`.

## Comportamento da interface

Detalhes em [contracts/ui-contract.md](./contracts/ui-contract.md).

- **Criar**: URL (obrigatória), código, data e hora de expiração. Envia `destinationUrl`, `code` (omitido se vazio) e `expiresAt` com deslocamento do fuso local. Sucesso: mostra o link e limpa o formulário. Rejeição: erro no campo, valores mantidos. Envio bloqueado em andamento.
- **Copiar**: copia `shortUrl`; mostra "Copiado!" ou aviso de falha com o link selecionável.
- **Consultar**: aceita código ou link completo; `200` → total; `404` → "não encontrado"; entrada preservada.
- **Link indisponível**: "Este link não existe ou expirou." com botão "Criar um novo link".
- **Acessibilidade**: rótulos, mensagens associadas aos campos, foco no primeiro erro, uso por teclado, layout fluido desde 320 px.

## Testes

| Tipo | Foco | Cobre |
|---|---|---|
| Unit (serviços) | `ShortLinkApiClient` (201/400/404/503/rede), `ProblemFieldMapper`, `ShortCodeParser`, `ExpirationConverter` (UTC−3, UTC+5:30, horário de verão) | FR-004, FR-005, FR-007, FR-009 |
| Unit (componentes, bUnit) | Erro por campo com valores preservados; limpeza após sucesso; botão bloqueado no envio; cópia ok/falha; consulta 200/404; `LinkUnavailable` | US1–US4 |
| Integração (existente) | 404 HTML vs JSON, 302 intacto, `/` entrega `index.html`, contrato das 4 mensagens | FR-008, FR-009, SC-005, SC-007 |
| E2E (Playwright) | Criar e copiar; rejeições por campo; consulta; abrir link inexistente/expirado; expiração em fuso não UTC | SC-001–SC-006 |

Os testes da feature 001 permanecem inalterados (SC-007). O E2E não repete regras de validação do backend: usa uma amostra por motivo de rejeição.

## Execução local

Passo a passo em [quickstart.md](./quickstart.md).

1. `dotnet run --project src/Encurtador.Api` (`http://localhost:5086`): serve API e UI.
2. Abrir `http://localhost:5086/`.
3. `dotnet test Encurtador.sln --filter "Category!=E2E"`; E2E conforme o quickstart.

## Complexity Tracking

| Desvio | Por que é necessário | Alternativa rejeitada |
|---|---|---|
| 2 novos projetos de teste | Isolar bUnit e Playwright | Tudo em `IntegrationTests`: mistura dependências e deixa execuções lentas |
| Mapeamento de erro por texto | A API não indica o campo e o contrato não muda | Adicionar `errors` ao ProblemDetails: fora do escopo (evolução recomendada) |
| Divergência spec × API em link expirado na consulta | A API devolve o total também para expirado | Alterar a API: fora do escopo; ajustar US3-2 e FR-006 da spec (ver research) |
