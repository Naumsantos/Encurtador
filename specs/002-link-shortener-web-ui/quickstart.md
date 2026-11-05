# Quickstart: validar a interface web

## Pré-requisitos

- SDK .NET 10 (10.0.401 ou superior).
- Para E2E: navegador Chromium do Playwright, instalado com o script gerado no build do projeto E2E (o `pwsh` não é necessário; use o Windows PowerShell).

## Build e execução

```powershell
dotnet build Encurtador.sln
dotnet run --project src/Encurtador.Api      # http://localhost:5086 (API + UI, mesma origem)
```

Abra `http://localhost:5086/`. `Links:PublicBaseUrl` (em `src/Encurtador.Api/appsettings.Development.json`) já é `http://localhost:5086`, a mesma origem que serve a UI; se mudar a porta, ajuste os dois.

Configuração do frontend: `src/Encurtador.Web/wwwroot/appsettings.json` (`Api:BaseUrl`, vazio = mesma origem).

## Testes

```powershell
dotnet test Encurtador.sln --filter "Category!=E2E"    # unitários, componentes e integração
# E2E (uma vez, após o build): instalar o Chromium
dotnet build tests/Encurtador.E2ETests
powershell -ExecutionPolicy Bypass -File tests/Encurtador.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet test tests/Encurtador.E2ETests --filter "Category=E2E"
```

Esperado: todos os testes da feature 001 continuam passando (SC-007).

## Validação manual dos fluxos

| # | Passos | Resultado esperado | Critério |
|---|---|---|---|
| 1 | Informar uma URL válida e criar | Link curto completo; botão Copiar; formulário limpo | US1, SC-001 |
| 2 | Clicar em Copiar | "Copiado!" em até 1 s | SC-003 |
| 3 | Criar com código `meu-link`; repetir o mesmo código | Segundo envio: "O código informado já está em uso." junto ao campo Código, valores mantidos | US2, SC-002/004 |
| 4 | URL `ftp://x`; código `ab`; expiração no passado | Cada motivo no campo correspondente, em português | FR-005 |
| 5 | Criar com expiração futura e abrir o link no navegador | Redireciona; consulta mostra acessos | US3 |
| 6 | Consultar pelo código e pelo link completo | Mesmo total | US3 |
| 7 | Abrir `/codigoinexistente` no navegador | Página "Este link não existe ou expirou." com botão para criar | US4, SC-005 |
| 8 | `curl -i http://localhost:5086/codigoinexistente` | 404 ProblemDetails JSON (inalterado) | SC-007 |
| 9 | `curl -i -H "Accept: text/html" http://localhost:5086/codigoinexistente` | 404 com HTML da SPA | FR-008 |
| 10 | Largura 320 px, só teclado | Todos os fluxos utilizáveis | SC-006 |

Detalhes de contratos: [contracts/http-integration.md](./contracts/http-integration.md), [contracts/ui-contract.md](./contracts/ui-contract.md); modelos: [data-model.md](./data-model.md).
