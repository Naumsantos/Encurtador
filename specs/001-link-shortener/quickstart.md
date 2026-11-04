# Quickstart: Encurtador de links

Este guia valida os fluxos públicos depois da implementação das tarefas. Veja [contrato OpenAPI](./contracts/openapi.yaml) para formas das requisições e respostas e [modelo de dados](./data-model.md) para invariantes persistidos.

## Pré-requisitos

- .NET 10 SDK.
- Ferramenta `dotnet-ef` disponível para aplicar as migrações locais.
- PowerShell e `curl.exe` (ou cliente HTTP equivalente).

## Preparar e executar

Na raiz do repositório, após os projetos e migrações serem criados:

```powershell
$env:ConnectionStrings__Links = "Data Source=encurtador.db;Cache=Shared;Default Timeout=2"
$env:Links__PublicBaseUrl = "http://localhost:5000"
dotnet restore
dotnet ef database update --project src/Encurtador.Infrastructure --startup-project src/Encurtador.Api
dotnet test
dotnet run --project src/Encurtador.Api --urls http://localhost:5000
```

Em desenvolvimento, `dotnet run` sem `--urls` usa o perfil `http` (`http://localhost:5086`), e `appsettings.Development.json` define `Links:PublicBaseUrl` com essa mesma URL. Os comandos acima sobrescrevem ambos para a porta 5000; mantenha as duas sempre iguais, ou o `shortUrl` retornado apontará para uma porta inacessível. No Postman, desative "Automatically follow redirects" para ver o `302`.

Configure `Links:PublicBaseUrl` com a URL canônica da implantação; ela é usada para compor o `shortUrl` retornado e não é obtida do cabeçalho `Host` da requisição.

## Cenários de validação

### Criar e acessar um código escolhido

Em outro terminal:

```powershell
$body = @{ destinationUrl = "https://example.com/docs"; code = "Ab_2" } | ConvertTo-Json -Compress
$created = Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/links -ContentType "application/json" -Body $body
$created
curl.exe -i --max-redirs 0 http://localhost:5000/Ab_2
```

Esperado: criação retorna `201` com `code`, `shortUrl` e destino; acesso retorna `302` com `Location: https://example.com/docs`.

### Consultar a contagem

```powershell
Invoke-RestMethod http://localhost:5000/api/links/Ab_2/stats
```

Esperado: resposta `200` com `accessCount` igual a 1 após o redirecionamento anterior. Repetir o GET `/Ab_2` eleva a contagem em exatamente um; consultar estatísticas não altera o total.

### Rejeitar dados inválidos e duplicidade

```powershell
$invalidBody = @{ destinationUrl = "file:///C:/invalid"; code = "Ab_2" } | ConvertTo-Json -Compress
try { Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/links -ContentType "application/json" -Body $invalidBody } catch { $_.Exception.Response.StatusCode }
$duplicateBody = @{ destinationUrl = "https://example.com/other"; code = "Ab_2" } | ConvertTo-Json -Compress
try { Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/links -ContentType "application/json" -Body $duplicateBody } catch { $_.Exception.Response.StatusCode }
```

Esperado: cada solicitação retorna `400` com `application/problem+json` e mensagem clara em português; a duplicidade não cria outro link.

### Verificar expiração e persistência

Crie um link com `expiresAt` no futuro, confirme que redireciona antes desse instante, e repita o acesso depois. Após expirar, o redirecionamento retorna `404` e o total permanece inalterado; a consulta de estatísticas continua disponível. Reinicie o processo mantendo o mesmo arquivo SQLite e confirme que destino, expiração e contagem permanecem.

## Testes automatizados que validam os limites arquiteturais

- `dotnet test` executa os testes unitários de `LinkService` e os testes de integração xUnit.
- Testes de integração usam SQLite real (arquivo temporário ou conexão em memória mantida aberta), não EF Core InMemory, para verificar o índice único e a atualização atômica.
- Teste o código automático quanto ao tamanho de 7 e ao alfabeto sem `0`, `1`, `I`, `L`, `O` ou `U`; exercite também colisões simultâneas e confirme que nenhum incremento válido é perdido.
- Testes de expiração cobrem instante anterior, igual e posterior ao limite; igualdade já conta como expirado.
