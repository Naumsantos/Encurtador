---
description: "Tarefas executáveis para implementar o encurtador de links"
---

# Tasks: Encurtador de links

**Input**: Design documents from `specs/001-link-shortener/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/openapi.yaml`

**Tests**: Incluídos porque a constituição exige cobertura xUnit para cada regra de negócio e testes de integração para o caminho de sucesso de cada endpoint.

**Organization**: As tarefas estão organizadas por história de usuário para permitir validação incremental.

## Phase 1: Setup

**Purpose**: Criar a solução .NET e os projetos previstos no plano.

- [X] T001 Criar `Encurtador.sln` e os projetos `src/Encurtador.Api/Encurtador.Api.csproj`, `src/Encurtador.Application/Encurtador.Application.csproj`, `src/Encurtador.Infrastructure/Encurtador.Infrastructure.csproj`, `tests/Encurtador.UnitTests/Encurtador.UnitTests.csproj` e `tests/Encurtador.IntegrationTests/Encurtador.IntegrationTests.csproj`, todos direcionados a `net10.0`.
- [X] T002 Adicionar referências entre os projetos em `src/Encurtador.Api/Encurtador.Api.csproj`, `src/Encurtador.Application/Encurtador.Application.csproj`, `src/Encurtador.Infrastructure/Encurtador.Infrastructure.csproj`, `tests/Encurtador.UnitTests/Encurtador.UnitTests.csproj` e `tests/Encurtador.IntegrationTests/Encurtador.IntegrationTests.csproj`; incluir EF Core SQLite e `Microsoft.EntityFrameworkCore.Design` na infraestrutura, xUnit nos projetos de teste e `Microsoft.AspNetCore.Mvc.Testing` nos testes de integração.
- [X] T003 Criar `Directory.Build.props` na raiz com configurações comuns de nullable reference types, implicit usings e análise de warnings compatíveis com os projetos .NET 10.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implementar o modelo e a persistência compartilhados que bloqueiam as três histórias.

- [X] T004 [P] Criar a entidade `ShortLink` em `src/Encurtador.Application/Links/ShortLink.cs` com `Id` do tipo `Guid`, `Code` requerido com máximo de 20 caracteres, `DestinationUrl` requerido com máximo de 2048 caracteres, `CreatedAtUtc` UTC obrigatório, `ExpiresAtUtc` UTC anulável e `AccessCount` `long` não negativo inicializado em zero.
- [X] T005 Criar o contrato de persistência `ILinkStore` em `src/Encurtador.Application/Persistence/ILinkStore.cs` com operações para verificar existência de código, salvar link com resultado neutro de colisão única, recuperar destino, incrementar acesso atomicamente somente para link ativo e consultar link para estatísticas.
- [X] T006 Criar `LinksDbContext` em `src/Encurtador.Infrastructure/Persistence/LinksDbContext.cs` e configuração EF Core em `src/Encurtador.Infrastructure/Persistence/ShortLinkConfiguration.cs`, incluindo chave primária, índice único nomeado em `Code`, limites de comprimento, collation de código sensível a maiúsculas/minúsculas e geração de `Guid` na aplicação.
- [X] T007 Implementar `EfLinkStore` em `src/Encurtador.Infrastructure/Persistence/EfLinkStore.cs`, traduzindo somente violações do índice único de código para o resultado de conflito neutro do contrato, propagando erros de persistência não relacionados, e expressando incremento como update SQL atômico sem carregar e reescrever `AccessCount`.
- [X] T008 Configurar SQLite, string `ConnectionStrings:Links`, timeout razoável de conexão e seleção de provider em `src/Encurtador.Infrastructure/DependencyInjection.cs`, sem SQL/provider específico na camada Application.
- [X] T009 Registrar `ILinkStore`, `LinksDbContext`, `LinkService`, `IShortCodeGenerator`, `LinkCreationValidator` e `TimeProvider` em `src/Encurtador.Api/Program.cs`; configurar ProblemDetails para validação HTTP 400 e indisponibilidade HTTP 503, com mensagens em português.
- [X] T010 Gerar a migração SQLite inicial em `src/Encurtador.Infrastructure/Persistence/Migrations/Sqlite/` com a tabela `ShortLink`, chave primária e índice único de código.
- [X] T011 [P] Criar `tests/Encurtador.IntegrationTests/Infrastructure/SqliteTestDatabase.cs` para prover banco SQLite relacional temporário aos testes, mantendo a conexão de memória aberta durante a execução quando essa modalidade for usada; não usar EF Core InMemory.

**Checkpoint**: Solução compila, banco SQLite aplica a migração inicial, e o contrato de persistência mantém as operações sensíveis ao provider fora de `LinkService`.

---

## Phase 3: User Story 1 - Criar e compartilhar um link curto (Priority: P1) 🎯 MVP

**Goal**: Criar links com código automático ou escolhido, destino validado e expiração futura opcional.

**Independent Test**: Enviar uma URL HTTP/HTTPS válida e confirmar `201`, código de 7 caracteres quando automático, `shortUrl` compartilhável e persistência; testar código escolhido, expiração e entradas inválidas sem criação parcial.

### Tests for User Story 1

- [X] T012 [P] [US1] Criar testes unitários de criação em `tests/Encurtador.UnitTests/Links/LinkServiceCreateTests.cs` cobrindo URLs HTTP/HTTPS absolutas até 2048 caracteres, rejeição acima de 2048 e de esquema inválido, códigos escolhidos de 4–20 caracteres ASCII com `[A-Za-z0-9_-]`, expiração estritamente futura, mensagens de erro em português e ausência de persistência em falha.
- [X] T013 [P] [US1] Criar testes unitários de geração e unicidade em `tests/Encurtador.UnitTests/Links/LinkServiceCodeGenerationTests.cs`, verificando código automático de exatamente 7 caracteres do alfabeto `23456789ABCDEFGHJKMNPQRSTVWXYZ`, repetição em colisão automática, rejeição em colisão escolhida e limite explícito de tentativas.
- [X] T014 [P] [US1] Criar teste de integração de sucesso de criação em `tests/Encurtador.IntegrationTests/Links/CreateLinkEndpointTests.cs`, usando SQLite real para validar `201`, corpo `CreatedLink`, cabeçalho `Location` e código persistido.
- [X] T015 [P] [US1] Criar testes de integração de validação em `tests/Encurtador.IntegrationTests/Links/CreateLinkValidationTests.cs` para ProblemDetails `400` em URL, código e expiração inválidos, e para duplicidade de código sem inserir um segundo link.

### Implementation for User Story 1

- [X] T016 [US1] Criar os contratos HTTP de criação em `src/Encurtador.Api/Contracts/CreateLinkRequest.cs` e `src/Encurtador.Api/Contracts/CreatedLinkResponse.cs`, com propriedades e nulabilidade alinhadas a `specs/001-link-shortener/contracts/openapi.yaml`.
- [X] T017 [US1] Implementar geração criptograficamente aleatória e validação de código em `src/Encurtador.Application/Links/ShortCodeGenerator.cs` e `src/Encurtador.Application/Links/LinkCreationValidator.cs`, usando o alfabeto automático `23456789ABCDEFGHJKMNPQRSTVWXYZ` e preservando regras mais amplas dos códigos escolhidos.
- [X] T018 [US1] Implementar criação em `src/Encurtador.Application/LinkService.cs`, recebendo `TimeProvider` e `ILinkStore`, normalizando datas para UTC, checando disponibilidade, tratando resultado neutro de colisão, repetindo apenas para código automático com limite definido e persistindo somente após validação completa.
- [X] T019 [US1] Implementar `POST /api/links` no arquivo único de recurso `src/Encurtador.Api/Endpoints/LinksEndpoints.cs`, retornar `201 Created` com `CreatedLinkResponse` e `Location` formado por `Links:PublicBaseUrl` configurada, e mapear validações para `application/problem+json` em português.
- [X] T020 [US1] Configurar `Links:PublicBaseUrl` obrigatória e validada na inicialização em `src/Encurtador.Api/Program.cs` e `src/Encurtador.Api/appsettings.json`, sem derivar a URL pública de um cabeçalho `Host` fornecido pelo cliente.

**Checkpoint**: A história de criação funciona de ponta a ponta e seus testes unitários e de integração passam.

---

## Phase 4: User Story 2 - Acessar um link e contabilizar o acesso (Priority: P1)

**Goal**: Redirecionar links válidos e incrementar exatamente uma vez por acesso válido, sem perda de incrementos concorrentes.

**Independent Test**: Criar/semear um link ativo, acessar seu código e confirmar `302` com `Location` correto; repetir e verificar aumento unitário; código desconhecido não redireciona nem altera contadores.

### Tests for User Story 2

- [X] T021 [P] [US2] Criar testes unitários em `tests/Encurtador.UnitTests/Links/LinkServiceRedirectTests.cs` cobrindo destino de código existente, código inexistente, delegação do incremento para a operação atômica do store e ausência de incremento quando não há link elegível.
- [X] T022 [P] [US2] Criar testes de integração em `tests/Encurtador.IntegrationTests/Links/RedirectLinkEndpointTests.cs` para sucesso `302`/`Location`, código inexistente `404`, contagem que aumenta em um por solicitação e requisições concorrentes contra SQLite real sem incremento perdido.

### Implementation for User Story 2

- [X] T023 [US2] Completar o caso de uso de redirecionamento em `src/Encurtador.Application/LinkService.cs`, recuperando o destino e invocando a operação de incremento atômico do store sem carregar nem regravar o contador.
- [X] T024 [US2] Implementar `GET /{code}` em `src/Encurtador.Api/Endpoints/LinksEndpoints.cs`, retornando redirecionamento HTTP 302 ao destino apenas quando a operação de acesso for válida e ProblemDetails 404 em português para código inexistente.
- [X] T025 [US2] Em `src/Encurtador.Infrastructure/Persistence/EfLinkStore.cs`, repetir SQLITE_BUSY/LOCKED no máximo três vezes com backoff limitado; mapear exaustão a HTTP 503 ProblemDetails em português em `src/Encurtador.Api/Endpoints/LinksEndpoints.cs` e testar que nunca retorna sucesso sem incremento.

**Checkpoint**: Redirecionamento ativo conta exatamente uma vez por solicitação válida; desconhecidos não contam.

---

## Phase 5: User Story 3 - Consultar acessos e respeitar a expiração (Priority: P2)

**Goal**: Consultar estatísticas publicamente, preservar dados entre reinicializações e impedir redirecionamento/contagem no instante de expiração ou depois dele.

**Independent Test**: Consultar contador sem autenticação; validar link antes, no instante e depois da expiração; confirmar que expiração não muda contagem, estatísticas ainda são consultáveis e os dados permanecem após reinício com o mesmo SQLite.

### Tests for User Story 3

- [X] T026 [P] [US3] Criar testes unitários de expiração em `tests/Encurtador.UnitTests/Links/LinkServiceExpirationTests.cs` para instante anterior, igual e posterior ao limite, rejeição de expiração igual/passada na criação e ausência de incremento após expirar.
- [X] T027 [P] [US3] Criar testes unitários de consulta em `tests/Encurtador.UnitTests/Links/LinkServiceStatsTests.cs`, verificando total acumulado, consulta de link expirado, código inexistente e que consultar estatísticas não incrementa contador.
- [X] T028 [P] [US3] Criar testes de integração em `tests/Encurtador.IntegrationTests/Links/LinkStatsEndpointTests.cs` para `200` público, corpo `LinkStats`, `404` para código inexistente e consulta de link expirado sem autenticação.
- [X] T029 [P] [US3] Criar testes de integração de expiração e persistência em `tests/Encurtador.IntegrationTests/Links/LinkExpirationPersistenceTests.cs`, verificando expiração condicional no update, nenhum redirect/contagem no instante do limite ou depois, e persistência de código, destino, expiração e contagem após recriar o host sobre o mesmo arquivo SQLite.

### Implementation for User Story 3

- [X] T030 [US3] Completar a operação de incremento em `src/Encurtador.Infrastructure/Persistence/EfLinkStore.cs` com predicado SQL de link ainda ativo (`ExpiresAtUtc` nulo ou estritamente maior que o instante UTC atual), de modo que o update retorne sucesso somente se incrementou um link elegível.
- [X] T031 [US3] Completar `LinkService` em `src/Encurtador.Application/LinkService.cs` para tratar update sem linha afetada como inexistente/expirado, mantendo expiração inclusiva no limite e expondo consulta pública do total sem mutação.
- [X] T032 [US3] Criar `src/Encurtador.Api/Contracts/LinkStatsResponse.cs` e mapear campos `code` e `accessCount` para o schema `LinkStats` de `specs/001-link-shortener/contracts/openapi.yaml`.
- [X] T033 [US3] Implementar `GET /api/links/{code}/stats` em `src/Encurtador.Api/Endpoints/LinksEndpoints.cs`, retornar `LinkStatsResponse` sem autenticação e sem incrementar acessos, inclusive para link expirado.

**Checkpoint**: Consulta pública retorna o total; links expirados permanecem consultáveis, mas nunca redirecionam nem recebem novos acessos contabilizados.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validar os contratos, documentação de execução e build completo sem ampliar o escopo.

- [X] T034 [P] Atualizar `specs/001-link-shortener/quickstart.md` para refletir os comandos reais de build, migração, configuração e execução após os projetos estarem implementados.
- [X] T035 [P] Validar `specs/001-link-shortener/contracts/openapi.yaml` contra as rotas, DTOs, status e schemas efetivamente implementados em `src/Encurtador.Api/Endpoints/LinksEndpoints.cs` e `src/Encurtador.Api/Contracts/`.
- [X] T036 Executar `dotnet restore`, `dotnet build Encurtador.sln` e `dotnet test Encurtador.sln`, corrigindo falhas relacionadas às três histórias antes de concluir.
- [X] T037 Executar os cenários reproduzíveis de `specs/001-link-shortener/quickstart.md` contra a API e SQLite persistente, verificando reinício, validações, redirecionamentos e estatísticas.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências; precisa ser concluída antes da fundação.
- **Foundational (Phase 2)**: depende de Setup e bloqueia as três histórias; fornece entidade, contexto, store, SQLite e tratamento comum de erros.
- **US1 (Phase 3)**: depende da fundação; entrega o MVP de criação.
- **US2 (Phase 4)**: depende da fundação e da entidade/store compartilhados; na execução sequencial vem após US1 porque completa `LinkService` e `LinksEndpoints.cs`.
- **US3 (Phase 5)**: depende da fundação e dos fluxos de contador/redirecionamento da US2; completa expiração e consulta estatística.
- **Polish (Phase 6)**: depende das histórias selecionadas para a entrega.

### User Story Dependencies

- **US1 (P1)**: independente após a fundação; MVP sugerido.
- **US2 (P1)**: conceitualmente independente após a fundação se houver um link semeado, mas a execução sequencial segue US1 para compartilhar os mesmos arquivos de serviço e endpoints.
- **US3 (P2)**: usa os dados e o contador persistidos pela US1/US2; implementar depois da US2 evita contratos parciais de expiração/estatísticas.

### Parallel Opportunities

- T004 pode ser implementada em paralelo com a configuração inicial dos projetos após T001; demais tarefas fundacionais que usam a entidade/store esperam os contratos definidos.
- Em cada história, arquivos de testes unitários e de integração distintos podem ser trabalhados em paralelo após o projeto de testes e o banco temporário estarem prontos.
- T034 e T035 são tarefas independentes por arquivo e podem ser paralelas; T036 e T037 requerem os artefatos e implementação finalizados.
- Evitar paralelizar tarefas de histórias que editam `LinkService.cs` ou `LinksEndpoints.cs`, pois são arquivos compartilhados.

### Parallel Example: User Story 1

```text
Após T001–T011:
T012 LinkServiceCreateTests.cs
T013 LinkServiceCodeGenerationTests.cs
T014 CreateLinkEndpointTests.cs
T015 CreateLinkValidationTests.cs
```

### Parallel Example: User Story 2

```text
Após T001–T020:
T021 LinkServiceRedirectTests.cs
T022 RedirectLinkEndpointTests.cs
```

### Parallel Example: User Story 3

```text
Após US1 e US2:
T026 LinkServiceExpirationTests.cs
T027 LinkServiceStatsTests.cs
T028 LinkStatsEndpointTests.cs
T029 LinkExpirationPersistenceTests.cs
```

## Implementation Strategy

### MVP First (User Story 1)

1. Concluir Setup e Foundational.
2. Implementar US1 com testes unitários e integração.
3. Validar `POST /api/links`, código automático/escolhido, validação, `Location` e persistência.
4. Parar e validar o MVP de criação antes de seguir para redirecionamento e estatísticas.

### Incremental Delivery

1. Setup + Foundational → solução compila e persiste em SQLite.
2. US1 → criação de link demonstrável.
3. US2 → links criados redirecionam e contam acessos atomicamente.
4. US3 → contagem pública, expiração e persistência após reinício.
5. Polish → contrato e quickstart conferidos contra o comportamento final.

## Task Format Validation

Todas as tarefas usam checkbox, ID sequencial, marcador `[P]` somente quando paralelizáveis, etiqueta `[US1]`/`[US2]`/`[US3]` nas fases de história e caminho(s) explícito(s) de arquivo na descrição.


## Phase 7: Convergence

- [X] T038 Alinhar `Links:PublicBaseUrl` à porta de desenvolvimento em `src/Encurtador.Api/appsettings.Development.json` (ou alinhar `src/Encurtador.Api/Properties/launchSettings.json`), de modo que o `shortUrl` retornado por `dotnet run` aponte para o endereço realmente acessível, e documentar em `specs/001-link-shortener/quickstart.md` per plan: canonical `Links:PublicBaseUrl` decision / US1/AC1 (partial)
- [X] T039 [P] Remover o arquivo placeholder `src/Encurtador.Infrastructure/Class1.cs`, não previsto no plano nem nas tarefas, per plan: Project Structure (unrequested)
- [X] T040 [P] Registrar `src/Encurtador.Api/ApiExceptionHandler.cs` (tratamento de JSON malformado como 400 ProblemDetails em português) em `specs/001-link-shortener/plan.md`, justificando-o pela Constitution V, ou removê-lo se não for desejado, per Constitution V (unrequested)