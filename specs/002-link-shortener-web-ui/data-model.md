# Data Model: Interface web do Encurtador

O frontend não persiste dados. Estes são os modelos em memória e DTOs de transporte. Os limites de negócio (tamanho da URL, formato do código, expiração futura) pertencem à API e **não** são validados aqui.

## CreateLinkModel (estado do formulário)

| Campo | Tipo | Regra no cliente |
|---|---|---|
| `DestinationUrl` | `string?` | Obrigatório (mensagem local "Informe a URL de destino."); `Trim()` antes do envio |
| `Code` | `string?` | Opcional; vazio/espaços → omitido no envio |
| `ExpirationDate` | `DateTime?` | Opcional (`MudDatePicker`) |
| `ExpirationTime` | `TimeSpan?` | Opcional (`MudTimePicker`) |

Regras de expiração (`ExpirationConverter`):
- Sem data e sem hora → `expiresAt` omitido.
- Só data → hora padrão 23:59.
- Só hora → erro local no campo de expiração ("Informe também a data de expiração.").
- Com data e hora → `DateTimeOffset` com o deslocamento do fuso **local do navegador** (via `IBrowserTimeZone`) na data informada, serializado em ISO 8601 (`"o"`).

Transições: `Editando → Enviando → (Sucesso: formulário limpo | Rejeitado: valores mantidos + erros por campo | Indisponível: valores mantidos + erro geral)`.

## CreatedLinkDto (resposta 201)

`Code: string`, `ShortUrl: string`, `DestinationUrl: string`, `CreatedAt: DateTimeOffset`, `ExpiresAt: DateTimeOffset?`.
Exibição: `ShortUrl` completo com botão Copiar; `ExpiresAt` mostrada no fuso local, se houver.

## LookupModel (consulta)

| Campo | Tipo | Regra |
|---|---|---|
| `Input` | `string?` | Obrigatório (vazio → "Informe o código ou o link curto."). `ShortCodeParser`: `Trim()`; se for URL absoluta, usa o último segmento do caminho; senão, usa o texto |

## LinkStatsDto (resposta 200)

`Code: string`, `AccessCount: long`. Exibição: "{N} acesso(s)" com singular e plural corretos.

## ApiResult&lt;T&gt;

União de quatro desfechos:

| Desfecho | Origem | Conteúdo |
|---|---|---|
| `Success` | 200/201 | `T Value` |
| `Rejected` | 400 ProblemDetails | `FormField Field` (`DestinationUrl`, `Code`, `Expiration`, `General`) e `string Message` (o `detail` da API) |
| `NotFound` | 404 | — |
| `Unavailable` | rede, timeout, 5xx, JSON ilegível | `string Message` genérica em português |

## FormField → campo da interface

| Mensagem da API (`detail`) | Campo |
|---|---|
| "Informe uma URL absoluta HTTP ou HTTPS válida com até 2048 caracteres." | `DestinationUrl` |
| "A data de expiração deve ser futura." | `Expiration` |
| "O código deve ter de 4 a 20 caracteres e conter apenas letras ASCII, números, hífen ou sublinhado." | `Code` |
| "O código informado já está em uso." | `Code` |
| Qualquer outra (inclusive JSON inválido) | `General` |
