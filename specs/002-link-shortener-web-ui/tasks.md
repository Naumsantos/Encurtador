---
description: "Tarefas executáveis para implementar a interface web do encurtador de links"
---

# Tasks: Interface web do Encurtador de links

**Input**: Design documents from `specs/002-link-shortener-web-ui/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/http-integration.md`, `contracts/ui-contract.md`

**Tests**: Incluídos porque a constituição exige xUnit para regras e um teste de integração para o caminho de sucesso de cada endpoint, e o plano define a estratégia de testes (D9).

**Organization**: Tarefas por história de usuário. Nomes de classes e arquivos em inglês; textos exibidos em português. Os testes da feature 001 não podem mudar de resultado (SC-007).

## Phase 1: Setup

**Purpose**: Criar os projetos novos e ligá-los à solution existente.

- [X] T001 Criar `src/Encurtador.Web/Encurtador.Web.csproj` (Blazor WebAssembly, `net10.0`, `Microsoft.AspNetCore.Components.WebAssembly` 10.0.12 e `MudBlazor` 9.x com versão fixada), mantendo `TreatWarningsAsErrors` herdado de `Directory.Build.props`; ajustar somente este projeto se pacotes WASM gerarem avisos (research §3.2).
- [X] T002 [P] Criar `tests/Encurtador.Web.UnitTests/Encurtador.Web.UnitTests.csproj` (`net10.0`, xUnit 2.9.3, `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, `bunit` 2.x, `<Using Include="Xunit" />`) com referência a `src/Encurtador.Web/Encurtador.Web.csproj`.
- [X] T003 [P] Criar `tests/Encurtador.E2ETests/Encurtador.E2ETests.csproj` (`net10.0`, xUnit 2.9.3, `Microsoft.Playwright` com versão fixada, `Microsoft.AspNetCore.Mvc.Testing`) com referência a `src/Encurtador.Api/Encurtador.Api.csproj`; todas as classes de teste E2E usam `[Trait("Category", "E2E")]`; o filtro oficial é `dotnet test Encurtador.sln --filter "Category!=E2E"` para a suíte padrão (sem navegador) e `dotnet test tests/Encurtador.E2ETests --filter "Category=E2E"` para o E2E, documentados no quickstart (research §3.4).
- [X] T004 Adicionar os três projetos novos ao `Encurtador.sln` com `dotnet sln Encurtador.sln add`.
- [X] T005 [P] Criar `src/Encurtador.Web/wwwroot/index.html` (`<html lang="pt-BR">`, `<base href="/">`, viewport, `div#app`, referência a `_framework/blazor.webassembly.js`, MudBlazor CSS/JS), `src/Encurtador.Web/wwwroot/appsettings.json` com `{ "Api": { "BaseUrl": "" } }`, `src/Encurtador.Web/wwwroot/appsettings.Development.json` e `src/Encurtador.Web/wwwroot/css/app.css`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Base compartilhada pelas quatro histórias: host, cliente HTTP, serviços de interop e hospedagem pela API.

- [X] T006 Criar `src/Encurtador.Web/Program.cs` (`WebAssemblyHostBuilder`, `AddMudServices()`, `AddHttpClient<IShortLinkApi, ShortLinkApiClient>` com base `Api:BaseUrl` ou `HostEnvironment.BaseAddress` quando vazio, registro de `IClipboardService` e `IBrowserTimeZone`), `src/Encurtador.Web/App.razor`, `src/Encurtador.Web/_Imports.razor` e `src/Encurtador.Web/Layout/MainLayout.razor` (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`, título da página em português).
- [X] T007 [P] Criar `src/Encurtador.Web/Models/CreatedLinkDto.cs` (`Code`, `ShortUrl`, `DestinationUrl`, `CreatedAt`, `ExpiresAt?`) e `src/Encurtador.Web/Models/LinkStatsDto.cs` (`Code`, `AccessCount` `long`), com nomes JSON camelCase conforme `contracts/http-integration.md`.
- [X] T008 [P] Criar `src/Encurtador.Web/Services/ApiResult.cs` com os desfechos `Success`, `Rejected(FormField Field, string Message)`, `NotFound` e `Unavailable(string Message)`, e o enum `FormField` (`DestinationUrl`, `Code`, `Expiration`, `General`), conforme `data-model.md`.
- [X] T009 Criar `src/Encurtador.Web/Services/IShortLinkApi.cs` e `src/Encurtador.Web/Services/ShortLinkApiClient.cs`: `CreateLinkAsync` (omite `code` vazio e `expiresAt` nulo) e `GetStatsAsync` (código com `Uri.EscapeDataString`); 201/200 → `Success`, 400 → `Rejected` via `ProblemFieldMapper`, 404 → `NotFound`, rede/timeout/5xx/JSON ilegível → `Unavailable`; `HttpClient.Timeout` = 15 segundos (FR-007), valor numa constante `ApiTimeout` com "Não foi possível falar com o servidor. Tente novamente.".
- [X] T010 [P] Criar `src/Encurtador.Web/Services/ProblemFieldMapper.cs` com correspondência exata do `detail` às quatro mensagens da tabela "FormField → campo" de `data-model.md`; qualquer outro texto (inclusive JSON inválido) → `General`.
- [X] T011 [P] Criar `src/Encurtador.Web/Services/IClipboardService.cs`, `src/Encurtador.Web/Services/ClipboardService.cs` e `src/Encurtador.Web/wwwroot/js/interop.js` (`navigator.clipboard.writeText`; retorna `false` se negado ou indisponível).
- [X] T012 [P] Criar `src/Encurtador.Web/Services/IBrowserTimeZone.cs`, `src/Encurtador.Web/Services/BrowserTimeZone.cs` e a função de fuso em `src/Encurtador.Web/wwwroot/js/interop.js` (`getTimezoneOffset` para o instante local informado).
- [X] T013 [P] Criar `src/Encurtador.Web/Services/ExpirationConverter.cs`: sem data e sem hora → `expiresAt` omitido; só data → hora padrão 23:59; só hora → erro local "Informe também a data de expiração."; data e hora → `DateTimeOffset` com o deslocamento local e serialização `"o"`.
- [X] T014 [P] Criar `src/Encurtador.Web/Services/ShortCodeParser.cs`: `Trim()`; se for URL absoluta, usa o último segmento do caminho sem barra final e sem query; caso contrário usa o texto; entrada vazia, ou código com caracteres fora de letras ASCII, números, hífen e sublinhado, ou URL sem segmento de caminho → resultado inválido (a página não chama a API).
- [X] T015 Atualizar `src/Encurtador.Api/Encurtador.Api.csproj` com referência a `..\Encurtador.Web\Encurtador.Web.csproj` e `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.12; atualizar `src/Encurtador.Api/Program.cs` com `UseDefaultFiles`, `UseBlazorFrameworkFiles` e `UseStaticFiles` antes de `MapLinkEndpoints()`, sem alterar rotas, status nem corpos JSON existentes. Justificar o pacote novo conforme `research.md`.
- [X] T016 [P] Criar `tests/Encurtador.Web.UnitTests/Infrastructure/BunitTestContext.cs` registrando `AddMudServices()`, `MudPopoverProvider` e `JSInterop` em modo solto, e fakes `FakeShortLinkApi`, `FakeClipboardService` e `FakeBrowserTimeZone` (research §3.3).

- [X] T017 Criar a fixture E2E em `tests/Encurtador.E2ETests/Infrastructure/E2EHostFixture.cs` (`IAsyncLifetime`, `ICollectionFixture` numa coleção `E2E`): escolher uma porta livre em `127.0.0.1`, criar um arquivo SQLite temporário e aplicar as migrações com `LinksDbContext.Database.MigrateAsync()`, iniciar `src/Encurtador.Api` num `WebApplication` real em Kestrel nessa porta com `ConnectionStrings:Links` apontando ao arquivo e `Links:PublicBaseUrl` igual à URL base, criar um `IBrowserContext` Playwright (Chromium, `Locale = "pt-BR"`, permissões de área de transferência concedidas, fuso configurável por teste) e, no descarte, encerrar a API e apagar o arquivo e os sufixos `-wal`/`-shm`. Expor `BaseUrl` e `NewPageAsync(timezoneId)`. Incluir `SeedLinkAsync(code, destinationUrl, expiresAtUtc, accessCount)` que grava direto em `LinksDbContext`; é assim que os testes obtêm um link **expirado** (a API rejeita expiração passada na criação) ou com contagem conhecida, sem esperar nem alterar o relógio. Todas as classes de teste E2E usam `[Trait("Category", "E2E")]` e `[Collection("E2E")]`.
**Checkpoint**: `dotnet build Encurtador.sln` compila com o Web hospedado pela API e `GET /` entrega o `index.html`; os 49 testes da 001 continuam passando.

---

## Phase 3: User Story 1 - Criar um link curto pela página (Priority: P1) 🎯 MVP

**Goal**: Criar links pela página, ver o link curto completo, copiá-lo e criar outro em seguida.

**Independent Test**: Abrir a página, enviar uma URL válida e verificar o link curto completo, o botão de copiar com confirmação, o formulário limpo e um novo envio sem recarregar.

### Tests for User Story 1

- [X] T018 [P] [US1] Criar `tests/Encurtador.Web.UnitTests/Services/ShortLinkApiClientCreateTests.cs` com `HttpMessageHandler` falso: 201 → `Success` com todos os campos, corpo enviado sem `code` quando vazio e com `expiresAt` em ISO 8601 com deslocamento.
- [X] T019 [P] [US1] Criar `tests/Encurtador.Web.UnitTests/Services/ExpirationConverterTests.cs` cobrindo omissão, só data (23:59), só hora (erro), UTC−3, UTC+5:30 e mudança de horário de verão.
- [X] T020 [P] [US1] Criar `tests/Encurtador.Web.UnitTests/Components/CreateLinkFormTests.cs` (bUnit): envio com apenas a URL exibe o link completo (US1/AC1), com código e expiração futura (AC2), copiar chama `IClipboardService` com o `shortUrl` e mostra "Copiado!" (AC3), falha de cópia mostra "Não foi possível copiar. Selecione o link e copie manualmente." e mantém o link visível, sucesso limpa todos os campos mantendo o link (AC4), novo envio funciona (AC5), botão desabilitado durante o envio e um segundo envio não chama a API duas vezes (FR-010).
- [X] T021 [P] [US1] Criar `tests/Encurtador.E2ETests/Links/CreateAndCopyTests.cs` (Playwright, usando `E2EHostFixture` da T017) com os `data-testid` de `ui-contract.md`: criar link, ver `short-url`, copiar e comparar o conteúdo da área de transferência com o link exibido (SC-003), criar com expiração em fuso não UTC.

### Implementation for User Story 1

- [X] T022 [P] [US1] Criar `src/Encurtador.Web/Models/CreateLinkModel.cs` com `DestinationUrl`, `Code`, `ExpirationDate` (`DateTime?`) e `ExpirationTime` (`TimeSpan?`); `Trim()` antes do envio e código vazio tratado como não informado.
- [X] T023 [P] [US1] Criar `src/Encurtador.Web/Components/CreatedLinkCard.razor`: `ShortUrl` completo (`data-testid="short-url"`), botão `copy-button` ("Copiar" → "Copiado!"), mensagem de falha de cópia, expiração exibida no fuso local quando houver.
- [X] T024 [US1] Criar `src/Encurtador.Web/Components/CreateLinkForm.razor` com `destination-url`, `custom-code`, `expiration-date`, `expiration-time` e `create-submit` (com `MudProgressCircular`); rótulos em português, URL vazia → "Informe a URL de destino."; flag `isSubmitting` que desabilita o botão; sucesso limpa os campos e exibe `CreatedLinkCard`; limpar mensagem e resultado anteriores a cada novo envio.
- [X] T025 [US1] Criar `src/Encurtador.Web/Pages/Home.razor` (rota `/`) com `CreateLinkForm` e `CreatedLinkCard`, título e layout fluido desde 320 px sem rolagem horizontal, mesmo com links longos.

**Checkpoint**: A criação funciona de ponta a ponta pela página e seus testes passam.

---

## Phase 4: User Story 2 - Entender e corrigir dados rejeitados (Priority: P1)

**Goal**: Mostrar o motivo em português junto ao campo certo e preservar tudo o que foi digitado.

**Independent Test**: Enviar URL inválida, código fora das regras, código já usado e expiração no passado e verificar a mensagem junto ao campo e os valores mantidos; simular serviço indisponível.

### Tests for User Story 2

- [X] T026 [P] [US2] Criar `tests/Encurtador.Web.UnitTests/Services/ProblemFieldMapperTests.cs` com as quatro mensagens conhecidas, a mensagem de JSON inválido e um texto desconhecido (→ `General`).
- [X] T027 [P] [US2] Criar `tests/Encurtador.Web.UnitTests/Services/ShortLinkApiClientErrorTests.cs`: 400 → `Rejected` com o campo correto, 5xx, exceção de rede, timeout e JSON ilegível → `Unavailable`; um teste com `HttpMessageHandler` que nunca responde confirma `Unavailable` ao fim de `ApiTimeout` (usar timeout reduzido injetado, sem esperar 15 s reais).
- [X] T028 [P] [US2] Criar `tests/Encurtador.Web.UnitTests/Components/CreateLinkFormErrorTests.cs` (bUnit): erro junto ao campo URL, código (formato e duplicidade) e expiração; valores preservados em toda rejeição (US2/AC1–AC4); erro geral em português com campos preservados e novo envio possível quando `Unavailable` (AC5); erro local para expiração só com hora.
- [X] T029 [P] [US2] Criar `tests/Encurtador.IntegrationTests/Hosting/ErrorMessageContractTests.cs` que cria links inválidos contra a API real e falha se as quatro mensagens de validação ou o formato 201/404 mudarem (research D3/D5), usando `LinkApiFactory` existente.
- [X] T030 [P] [US2] Criar `tests/Encurtador.E2ETests/Links/RejectionTests.cs` com uma amostra por motivo (URL inválida, código inválido, código duplicado, expiração no passado), conferindo mensagem junto ao campo e valores mantidos (SC-002).

### Implementation for User Story 2

- [X] T031 [US2] Atualizar `src/Encurtador.Web/Components/CreateLinkForm.razor` para tratar `Rejected`/`Unavailable`: exibir o `detail` da API junto ao campo mapeado (`MudTextField` `Error`/`ErrorText`, associado ao campo via `aria-describedby`), mensagem geral para `General` e `Unavailable`, mover o foco para o primeiro campo com erro e nunca limpar valores em falha.
- [X] T032 [US2] Garantir em `src/Encurtador.Web/Components/CreateLinkForm.razor` que `ExpirationConverter` e a conversão de fuso rodem antes do envio e que o erro local de expiração apareça junto ao campo de expiração, sem chamar a API.

**Checkpoint**: Todas as rejeições mostram mensagem em português junto ao campo correto, sem perda de dados.

---

## Phase 5: User Story 3 - Consultar o total de acessos (Priority: P2)

**Goal**: Consultar o total de acessos por código ou link curto completo, sem login.

**Independent Test**: Consultar um link existente, um link expirado (continua mostrando o total), um código inexistente e um link colado completo.

### Tests for User Story 3

- [X] T033 [P] [US3] Criar `tests/Encurtador.Web.UnitTests/Services/ShortCodeParserTests.cs`: código simples, link completo com e sem barra final, com porta, com query, texto com espaços nas extremidades, entrada vazia e texto que não é código nem link (resultado inválido, sem exceção).
- [X] T034 [P] [US3] Criar `tests/Encurtador.Web.UnitTests/Services/ShortLinkApiClientStatsTests.cs`: 200 → `Success`, 404 → `NotFound`, 5xx e rede → `Unavailable`.
- [X] T035 [P] [US3] Criar `tests/Encurtador.Web.UnitTests/Components/StatsLookupFormTests.cs` (bUnit): total com singular/plural ("1 acesso", "N acessos"), "Nenhum link encontrado para esse código." junto ao campo com o código mantido, "Informe o código ou o link curto." para entrada vazia, "Informe um código ou um link curto válido." para texto inválido sem chamar `IShortLinkApi`, erro de consulta não altera o formulário de criação nem o link exibido, consulta de link recém-criado.
- [X] T036 [P] [US3] Criar `tests/Encurtador.E2ETests/Links/LookupTests.cs` (usa `E2EHostFixture`; semeia com `SeedLinkAsync`): consultar código existente com contagem conhecida, código inexistente, link completo colado e link expirado semeado com `expiresAtUtc` no passado (o total é exibido).

### Implementation for User Story 3

- [X] T037 [P] [US3] Criar `src/Encurtador.Web/Models/LookupModel.cs` e `src/Encurtador.Web/Components/StatsLookupForm.razor` com `lookup-input`, `lookup-submit` e `lookup-result` (`MudAlert`), usando `ShortCodeParser` (entrada inválida → "Informe um código ou um link curto válido." junto ao campo, sem chamar a API) e `IShortLinkApi.GetStatsAsync`, estado de envio que impede consultas duplicadas simultâneas, mensagens do `ui-contract.md` e entrada sempre preservada.
- [X] T038 [US3] Atualizar `src/Encurtador.Web/Pages/Home.razor` para incluir `StatsLookupForm` independente do formulário de criação, anunciando o resultado a leitores de tela (`aria-live`).

**Checkpoint**: A consulta funciona com código ou link completo e não interfere na criação.

---

## Phase 6: User Story 4 - Página para link inexistente ou expirado (Priority: P2)

**Goal**: Navegadores veem uma página clara em português para links inexistentes ou expirados; clientes de API não mudam.

**Independent Test**: Abrir no navegador um código inexistente e um link expirado e ver a página com "Criar um novo link"; `curl` e `Accept: application/json` continuam recebendo ProblemDetails JSON; links válidos continuam em 302.

### Tests for User Story 4

- [X] T039 [P] [US4] Criar `tests/Encurtador.IntegrationTests/Hosting/BrowserNotFoundPageTests.cs` (`LinkApiFactory`): `GET /{code}` inexistente e expirado com `Accept: text/html` → 404 `text/html` com o `index.html`; com `application/json`, `*/*` e sem `Accept` → 404 ProblemDetails JSON idêntico ao atual; link válido → 302 inalterado; `GET /` → 200 com `index.html`; 503 continua JSON.
- [X] T040 [P] [US4] Criar `tests/Encurtador.Web.UnitTests/Components/LinkUnavailableTests.cs` (bUnit): texto "Este link não existe ou expirou." e botão "Criar um novo link" (`new-link-button`) que navega para `/`.
- [X] T041 [P] [US4] Criar `tests/Encurtador.E2ETests/Links/UnavailableLinkTests.cs`: abrir no navegador código inexistente e link expirado (semeado com `E2EHostFixture.SeedLinkAsync` e `expiresAtUtc` no passado), ver a página em português sem redirecionamento, acionar o botão e chegar ao formulário (SC-005).

### Implementation for User Story 4

- [X] T042 [US4] Criar `src/Encurtador.Api/Endpoints/BrowserNotFoundPageFilter.cs` (`IEndpointFilter`): quando o resultado for o 404 "Link não encontrado" e `Accept` incluir `text/html`, responder 404 com o conteúdo de `index.html` obtido de `WebRootFileProvider`; qualquer outro caso devolve o resultado original sem alteração.
- [X] T043 [US4] Aplicar `.AddEndpointFilter<BrowserNotFoundPageFilter>()` somente a `GET /{code}` em `src/Encurtador.Api/Endpoints/LinksEndpoints.cs`, sem tocar em `POST /api/links` nem em `GET /api/links/{code}/stats`.
- [X] T044 [P] [US4] Criar `src/Encurtador.Web/Pages/LinkUnavailable.razor` (rota `/{*path}`) com "Este link não existe ou expirou." e `MudButton` `new-link-button` ("Criar um novo link") levando a `/`.

**Checkpoint**: A página aparece no navegador para links indisponíveis e nenhuma resposta existente da API muda.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Acessibilidade, configuração, documentação e verificação final.

- [X] T045 [P] Revisar `src/Encurtador.Web/Components/` e `src/Encurtador.Web/Pages/` quanto a FR-011/FR-012: todos os textos em português, rótulos em todos os campos, ordem de foco por teclado, foco após erro e resultado, anúncio de erro e de "Copiado!" a leitores de tela, contraste e alvos de toque adequados, `lang="pt-BR"`.
- [X] T046 [P] Criar `tests/Encurtador.E2ETests/Links/AccessibilityLayoutTests.cs`: fluxos principais em 320 px sem rolagem horizontal e apenas por teclado (SC-006).
- [X] T047 [P] Atualizar `specs/002-link-shortener-web-ui/quickstart.md` com os comandos reais: build, `dotnet run --project src/Encurtador.Api`, instalação dos navegadores (`pwsh tests/Encurtador.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium`) e os dois filtros: `dotnet test Encurtador.sln --filter "Category!=E2E"` (padrão) e `dotnet test tests/Encurtador.E2ETests --filter "Category=E2E"` (E2E).
- [X] T048 Verificar `Links:PublicBaseUrl` em `src/Encurtador.Api/appsettings.Development.json` igual à origem que serve a UI e documentar no quickstart.
- [X] T049 Executar `dotnet restore`, `dotnet build Encurtador.sln` e `dotnet test Encurtador.sln --filter "Category!=E2E"` confirmando que os 49 testes da feature 001 mantêm o resultado (SC-007); corrigir falhas relacionadas.
- [X] T050 Executar `dotnet test tests/Encurtador.E2ETests --filter "Category=E2E"` (após instalar os navegadores) e os cenários do `quickstart.md` contra a API real, validando SC-001 a SC-006.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende da Setup; bloqueia as quatro histórias. T006 depende de T009–T012 (registra os serviços que elas criam); T017 (fixture E2E) bloqueia todas as tarefas de teste E2E.
- **US1 (Phase 3)**: depende da fundação; MVP.
- **US2 (Phase 4)**: depende da US1 porque altera `CreateLinkForm.razor`.
- **US3 (Phase 5)**: depende da fundação; edita `Home.razor` após a US1.
- **US4 (Phase 6)**: depende da fundação (hospedagem T015); independente das demais.
- **Polish (Phase 7)**: depende das histórias selecionadas.

### Parallel Opportunities

- T002, T003 e T005 em paralelo após T001; T007, T008, T010–T014 e T016 em paralelo na fundação.
- Em cada história, os arquivos de teste (marcados `[P]`) são independentes e podem ser escritos juntos.
- US3 (T037) e US4 (T042–T044) podem andar em paralelo com a US2, pois tocam arquivos diferentes.
- Evitar paralelizar tarefas que editam `CreateLinkForm.razor`, `Home.razor` ou `LinksEndpoints.cs`.

### Parallel Example: User Story 1

```text
Após T001–T017:
T018 ShortLinkApiClientCreateTests.cs
T019 ExpirationConverterTests.cs
T020 CreateLinkFormTests.cs
T022 CreateLinkModel.cs
T023 CreatedLinkCard.razor
```

## Implementation Strategy

### MVP First (User Story 1)

1. Concluir Setup e Foundational.
2. Implementar US1 com testes.
3. Validar criação, cópia e novo envio; parar e validar o MVP.

### Incremental Delivery

1. Setup + Foundational → UI hospedada pela API.
2. US1 → criar e copiar.
3. US2 → erros por campo, sem perda de dados.
4. US3 → consulta de acessos.
5. US4 → página de link indisponível.
6. Polish → acessibilidade, documentação e verificação final.

## Task Format Validation

Todas as tarefas usam checkbox, ID sequencial, `[P]` somente quando paralelizáveis, etiqueta `[US1]`–`[US4]` nas fases de história e caminho explícito de arquivo.
