# Encurtador de links

API REST para criação e gerenciamento de links curtos, construída com .NET 10,
C# e Minimal APIs. O repositório também contém a especificação da feature e as
decisões de arquitetura em `specs/001-link-shortener/`.

## Estado atual

A API implementa:

- Criação de links com código personalizado ou gerado automaticamente.
- Validação da URL, do código e da data de expiração.
- Redirecionamento para o destino e contagem de acessos.
- Consulta de estatísticas por código.
- Persistência com EF Core e SQLite.
- Respostas de erro no formato ProblemDetails.
- Documento OpenAPI gerado em ambiente de desenvolvimento.

O escopo da feature da API está concluído: as 40 tarefas em
`specs/001-link-shortener/tasks.md` estão marcadas como feitas. No momento, a
solução contém a API, as camadas Application e Infrastructure e projetos de
testes; ainda não contém o frontend.

## Tecnologias

- .NET 10 e C#
- ASP.NET Core Minimal APIs
- Entity Framework Core com SQLite
- xUnit para testes unitários e de integração

## Estrutura

| Caminho | Responsabilidade |
| --- | --- |
| `src/Encurtador.Api` | Endpoints HTTP, contratos, configuração e tratamento de erros |
| `src/Encurtador.Application` | Serviço de aplicação, validações e regras de negócio |
| `src/Encurtador.Infrastructure` | Persistência EF Core e migrações SQLite |
| `tests/Encurtador.UnitTests` | Testes unitários da aplicação |
| `tests/Encurtador.IntegrationTests` | Testes de integração da API e persistência |
| `specs/001-link-shortener` | Especificação, plano, tarefas e contrato OpenAPI |

## Pré-requisitos

- .NET 10 SDK.
- `dotnet-ef` 10.x para aplicar as migrações do banco.
- PowerShell.

Se ainda não tiver a ferramenta EF instalada, instale uma versão compatível com
EF Core 10:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
```

## Restaurar, preparar o banco e executar

Execute os comandos na raiz do repositório:

```powershell
$env:ConnectionStrings__Links = "Data Source=encurtador.db;Cache=Shared;Default Timeout=2"
$env:Links__PublicBaseUrl = "http://localhost:5000"

dotnet restore
dotnet ef database update --project src/Encurtador.Infrastructure --startup-project src/Encurtador.Api
dotnet run --project src/Encurtador.Api --urls http://localhost:5000
```

Mantenha `Links__PublicBaseUrl` igual à URL e à porta usadas para executar a API:
esse valor compõe o `shortUrl` retornado na criação. A connection string acima
cria/usa `encurtador.db` no diretório de execução.

O documento OpenAPI fica disponível em ambiente de desenvolvimento em:

```text
http://localhost:5000/openapi/v1.json
```

Atualmente a aplicação expõe o documento OpenAPI, mas não configura uma interface
Swagger UI.

## Endpoints

| Método | Rota | Descrição |
| --- | --- | --- |
| `POST` | `/api/links` | Cria um link curto |
| `GET` | `/{code}` | Redireciona para o destino e incrementa os acessos |
| `GET` | `/api/links/{code}/stats` | Consulta a contagem de acessos |

Exemplo de criação:

```powershell
$body = @{
    destinationUrl = "https://example.com/docs"
    code = "Ab_2"
} | ConvertTo-Json -Compress

Invoke-RestMethod `
    -Method Post `
    -Uri http://localhost:5000/api/links `
    -ContentType "application/json" `
    -Body $body
```

Para inspecionar a resposta `302` sem seguir o redirecionamento:

```powershell
curl.exe -i --max-redirs 0 http://localhost:5000/Ab_2
```

## Testes

Execute todos os testes da solução:

```powershell
dotnet test Encurtador.sln
```

Os testes de integração usam SQLite para validar os endpoints e a persistência.
Na última validação deste README, os 49 testes existentes passaram: 35 unitários
e 14 de integração.

## O que falta fazer

1. **Criar o frontend** em `src/Encurtador.Web`, na mesma solution, usando
   Blazor Server no .NET 10 e MudBlazor como biblioteca de UI.
2. **Integrar o frontend com a API documentada**. Componentes `.razor` devem ficar
   restritos à apresentação; chamadas HTTP e estado compartilhado devem ficar em
   serviços injetados. Regras de negócio permanecem fora do frontend.
3. **Implementar e testar a experiência das telas**: estados de carregamento,
   erro e vazio; textos apresentados ao usuário em português.
4. **Adicionar testes do frontend**: bUnit com xUnit para componentes com
   interações críticas e testes E2E com Playwright para os fluxos principais.
5. **Revisar a documentação da API**: confirmar a cobertura dos endpoints no
   OpenAPI e decidir se também será necessária uma interface Swagger UI ou a
   publicação do documento fora de Development.

Os detalhes funcionais da API existente estão em
[`specs/001-link-shortener/spec.md`](specs/001-link-shortener/spec.md), e o
contrato está em
[`specs/001-link-shortener/contracts/openapi.yaml`](specs/001-link-shortener/contracts/openapi.yaml).
