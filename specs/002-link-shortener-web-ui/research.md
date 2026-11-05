# Research: Interface web do Encurtador de links

Esta pesquisa registra o que foi verificado no código da feature 001 e as decisões que dele decorrem. Nada aqui altera regras de negócio ou contratos de dados da API.

## 1. Estado real do backend (inspecionado)

| Item | Fato verificado | Fonte |
|---|---|---|
| Solution | `Encurtador.sln` com `src/Encurtador.{Api,Application,Infrastructure}` e `tests/Encurtador.{UnitTests,IntegrationTests}`; `Directory.Build.props` ativa `TreatWarningsAsErrors` e `AnalysisLevel=latest` | `Encurtador.sln`, `Directory.Build.props` |
| Criar link | `POST /api/links`, corpo JSON `{ destinationUrl, code?, expiresAt? }` (`expiresAt` é `DateTimeOffset?`). Sucesso: `201` com `{ code, shortUrl, destinationUrl, createdAt, expiresAt }` e `Location` | `CreateLinkRequest.cs`, `CreatedLinkResponse.cs`, `LinksEndpoints.cs` |
| Rejeição | `400` `application/problem+json` com `title: "Solicitação inválida"` e `detail` em português. Não existe extensão `errors` nem nome de campo | `LinksEndpoints.cs`, `ApiExceptionHandler.cs` |
| Mensagens de `detail` (únicas 4 de validação) | URL: "Informe uma URL absoluta HTTP ou HTTPS válida com até 2048 caracteres."; expiração: "A data de expiração deve ser futura."; código: "O código deve ter de 4 a 20 caracteres e conter apenas letras ASCII, números, hífen ou sublinhado." e "O código informado já está em uso."; corpo malformado: "O corpo da solicitação contém JSON inválido ou dados obrigatórios ausentes." | `LinkCreationValidator.cs`, `LinkService.cs`, `ApiExceptionHandler.cs` |
| Ordem de validação | URL, depois expiração, depois formato do código, depois unicidade: a API devolve **apenas o primeiro** erro | `LinkCreationValidator.Validate` |
| Consultar acessos | `GET /api/links/{code}/stats` → `200 { code, accessCount }`; inexistente → `404` ProblemDetails ("Link não encontrado") | `LinksEndpoints.cs`, `LinkStatsResponse.cs` |
| Consulta de link expirado | `GetStatsAsync` **não verifica expiração**: link expirado retorna `200` com o total | `EfLinkStore.GetStatsAsync` |
| Redirecionar | `GET /{code}` → `302` para o destino, ou `404` ProblemDetails JSON ("Link não encontrado") tanto para código inexistente quanto para expirado; `503` ProblemDetails se o banco estiver ocupado | `LinksEndpoints.cs` |
| Configuração | `Links:PublicBaseUrl` (`http://localhost:5086` em Development, `http://localhost:5000` na base); `launchSettings` usa `http://localhost:5086` | `appsettings*.json`, `launchSettings.json` |
| CORS / arquivos estáticos | Nenhum configurado. O `Program.cs` só mapeia os três endpoints | `Program.cs` |
| OpenAPI | Gerado em runtime apenas em Development (`MapOpenApi`) e mantido à mão em `specs/001-link-shortener/contracts/openapi.yaml` | `Program.cs`, spec 001 |

### Divergência entre a spec 002 e o backend

A US3 / cenário 2 e o FR-006 da spec 002 pedem "não existe ou expirou" ao consultar acessos. A API devolve o total mesmo para link expirado e só responde 404 para código inexistente. Como o contrato não pode mudar, a interface mostrará: total quando existir (expirado ou não) e "Nenhum link encontrado para esse código." quando 404. **Ação de acompanhamento**: ajustar o texto da spec 002 (US3-2 e FR-006) para "não existe" na consulta de acessos; a mensagem "não existe ou expirou" permanece para a página de link aberto no navegador.

## 2. Decisões

### D1 — Hospedagem: a API serve a aplicação WebAssembly (mesma origem)

- **Decisão**: `Encurtador.Api` serve os arquivos do `Encurtador.Web` (`UseBlazorFrameworkFiles` + `UseStaticFiles` + `UseDefaultFiles`). A UI e os links curtos compartilham a origem `Links:PublicBaseUrl`.
- **Motivo**: o `shortUrl` devolvido usa `PublicBaseUrl` e a rota `/{code}` pertence à API. Para a página de erro aparecer quando alguém abre `https://host/abc123`, a resposta a essa URL precisa vir do mesmo host que atende o redirecionamento. Uma aplicação WASM em outro host jamais seria consultada para essas URLs. A mesma origem também elimina CORS.
- **Alternativas**: (a) WASM em host separado + CORS: não resolve a página de erro e exigiria que o `PublicBaseUrl` apontasse para um proxy; (b) proxy reverso decidindo por status 404: infraestrutura fora do repositório e sem ganho; (c) `Blazor Web App` com SSR: contradiz a decisão de usar WebAssembly.
- **Impacto**: nova dependência no `Encurtador.Api` (`Microsoft.AspNetCore.Components.WebAssembly.Server`) e referência de projeto ao `Encurtador.Web`.

### D2 — Página de link inexistente/expirado

- **Problema**: `/{code}` responde 404 em JSON; a UI WASM só roda se o navegador receber `index.html`.
- **Decisão**: um filtro de endpoint (`BrowserNotFoundPageFilter`) aplicado somente à rota `GET /{code}`. Quando o resultado for o 404 de "Link não encontrado" **e** o cabeçalho `Accept` incluir `text/html` (navegação de browser), responde com o conteúdo de `index.html` e status 404. Para qualquer outro `Accept` (inclusive `*/*`, usado por `curl` e `HttpClient`), o 404 ProblemDetails permanece idêntico.
- **Roteamento no cliente**: a aplicação possui duas rotas: `/` (Home) e `/{*path}` (página "link indisponível"). Como o host só entrega a aplicação em `/` e nas respostas 404 de `/{code}`, qualquer outro caminho carregado no navegador significa um código que não resolveu. Não há caminho reservado; assim nenhum código personalizado válido (ex.: `consultar`, `link-indisponivel`) é bloqueado, o que alteraria regra de negócio.
- **Alternativas**: rota reservada `/link-indisponivel` (rejeitada: conflita com códigos válidos de 4–20 caracteres); `UseStatusCodePagesWithReExecute` (rejeitada: só atua em respostas sem corpo e o endpoint já escreve ProblemDetails); alterar o endpoint para devolver HTML (rejeitada: mistura apresentação ao handler).
- **Impacto**: mudança mínima no backend, aditiva e restrita ao hospedeiro; nenhum contrato JSON muda. O 404 do navegador não distingue inexistente de expirado, pois a API também não distingue; a página usa a mensagem "não existe ou expirou" exigida pela spec.
- **Limite aceito**: o 503 (banco ocupado) em navegação continua sendo ProblemDetails JSON; está fora da spec.

### D3 — Cliente HTTP: tipado e escrito à mão

- **Decisão**: `ShortLinkApiClient`, um `HttpClient` tipado registrado por `AddHttpClient<IShortLinkApi, ShortLinkApiClient>`, com DTOs próprios no projeto Web.
- **Motivo**: o OpenAPI existe só em runtime (Development) e em YAML mantido à mão; geradores (NSwag/Kiota) acrescentariam dependência e etapa de build para dois endpoints. A constituição exige justificativa por dependência nova.
- **Contratos protegidos por teste**: um teste de integração no projeto existente (`Encurtador.IntegrationTests`) verifica que as 4 mensagens de validação e o formato 201/404 continuam como o cliente espera (seção "Mapeamento de erros" abaixo).

### D4 — Configuração da URL base da API

- **Decisão**: chave `Api:BaseUrl` em `wwwroot/appsettings.json` do Web (valor vazio = mesma origem, via `HostEnvironment.BaseAddress`), com sobrescrita por `wwwroot/appsettings.{Ambiente}.json`.
- **Motivo**: appsettings de WASM é baixado em tempo de execução, então o valor muda por ambiente sem recompilar. Como a UI é servida pela API, o padrão de mesma origem é o mais seguro; um valor diferente exigiria CORS na API e fica documentado como fora do caminho principal.

### D5 — Mapeamento de erros para campos

- **Decisão**: `ProblemFieldMapper` associa `ProblemDetails.detail` ao campo comparando com as quatro mensagens conhecidas (URL, expiração, código ×2). O que não corresponder (incluindo corpo inválido e `503`) vira mensagem geral em português, e o campo "URL" nunca recebe um erro inesperado.
- **Motivo**: a API não informa o campo e não pode mudar. Comparar mensagens é frágil por natureza; o teste de contrato (D3) falha se o texto do backend mudar, e a alternativa duradoura (adicionar a extensão `errors` ao ProblemDetails) fica como recomendação de evolução, não como parte desta feature.
- **Regras de negócio**: a UI valida localmente apenas campo obrigatório (URL vazia) e normaliza entrada (trim; código/expiração vazios viram "não informado"). Limites de tamanho, esquema, formato de código e "expiração futura" são decididos exclusivamente pela API (constituição II).

### D6 — Data de expiração e fuso do usuário

- **Fato**: em Blazor WebAssembly `TimeZoneInfo.Local` normalmente é UTC; não reflete o fuso do navegador.
- **Decisão**: `MudDatePicker` + `MudTimePicker` coletam data e hora locais. Um serviço `IBrowserTimeZone` chama uma função JS (`getTimezoneOffset` para aquele instante local) e monta um `DateTimeOffset` com o deslocamento correto, serializado em ISO 8601 (`"o"`), formato aceito por `DateTimeOffset?` na API. Só data informada → hora padrão 23:59; só hora → erro no campo de expiração gerado localmente ("Informe a data.").
- **Alternativa**: `<input type="datetime-local">` do MudBlazor: entrega string sem offset, com o mesmo problema.

### D7 — Cópia para a área de transferência

- **Decisão**: `IClipboardService` sobre `navigator.clipboard.writeText` via JS interop; retorna sucesso/falha. A UI mostra `Snackbar` + rótulo "Copiado!" no botão em caso de sucesso, e mensagem de falha com o link selecionável em caso de erro (contexto inseguro ou permissão negada).

### D8 — Estrutura do Web e estado

- Serviços em `Services/`, componentes sem `HttpClient`. Estado de página em *view models* simples (`CreateLinkModel`, `LookupModel`) com `EditForm`/`MudForm`; envio protegido por flag `isSubmitting` para impedir duplicidade (FR-010).
- Consulta de acessos aceita código ou link completo: `ShortCodeParser` extrai o último segmento quando a entrada for uma URL absoluta; caso contrário usa o texto como está (trim).

### D9 — Estratégia de testes

| Camada | Ferramenta | Projeto | Escopo |
|---|---|---|---|
| Serviços e mapeadores | xUnit + `HttpMessageHandler` falso | `tests/Encurtador.Web.UnitTests` (novo) | `ShortLinkApiClient`, `ProblemFieldMapper`, `ShortCodeParser`, `ExpirationConverter` |
| Componentes | bUnit 2.x (`BunitContext`) + MudBlazor | `tests/Encurtador.Web.UnitTests` | formulário (erros por campo, preservação de valores, limpeza após sucesso, estado de envio), cópia, consulta, página indisponível |
| Hospedagem/contrato | `WebApplicationFactory<Program>` | `tests/Encurtador.IntegrationTests` (existente) | filtro HTML 404 vs JSON 404, `/` entrega `index.html`, mensagens que o mapeador usa |
| Ponta a ponta | Microsoft.Playwright + xUnit | `tests/Encurtador.E2ETests` (novo) | criar+copiar, rejeições por campo, consulta, link inexistente no navegador, fuso na expiração |

- bUnit 2.x não depende de adaptador xUnit v2 (usa `BunitContext` direto), compatível com o xUnit 2.9.3 já adotado.
- O E2E sobe o `Encurtador.Api` real em porta livre com SQLite temporário (Playwright precisa de servidor Kestrel real, não `TestServer`) e não repete as regras já cobertas pelos testes de integração do backend.

### Dependências novas e justificativa

| Pacote | Projeto | Justificativa |
|---|---|---|
| `Microsoft.AspNetCore.Components.WebAssembly` 10.0.x | `Encurtador.Web` | Runtime Blazor WebAssembly exigido pela decisão de stack |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` 10.0.x | `Encurtador.Api` | `UseBlazorFrameworkFiles` para servir o WASM pela API (D1) |
| `MudBlazor` 9.x | `Encurtador.Web` | Biblioteca de componentes exigida pela decisão de stack (formulário, picker, snackbar) |
| `bunit` 2.x | `Encurtador.Web.UnitTests` | Testes de componentes Blazor |
| `Microsoft.Playwright` 1.6x | `Encurtador.E2ETests` | Navegador real: fuso, área de transferência e página 404 só se validam assim |
| `Microsoft.AspNetCore.Components.WebAssembly.DevServer` | não adotado | A API já hospeda; dispensa servidor de desenvolvimento separado |

Versões foram verificadas no NuGet em 2026-11-04 (MudBlazor 9.12.0, bunit 2.13.3, Playwright 1.64.1, Blazor 10.0.14); fixar as exatas na implementação, alinhando os pacotes ASP.NET Core à versão já usada pelo repositório (10.0.12).

## 3. Riscos e pontos a validar na implementação

1. `index.html` precisa ser resolvido por `WebRootFileProvider` da API em `dotnet run` e em `publish`; validar com teste de integração.
2. `TreatWarningsAsErrors` pode expor avisos nos pacotes WASM; ajustar `Encurtador.Web.csproj` sem afrouxar os demais projetos.
3. MudBlazor 9 + bUnit exige registrar `AddMudServices()` e um `MudPopoverProvider` no contexto; centralizar em um helper de teste.
4. Sem `playwright install` o E2E não roda; o projeto fica fora do alvo padrão de `dotnet test` da solution (marcado por trait) e documentado no quickstart.
