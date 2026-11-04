# Feature Specification: Encurtador de links

**Feature Branch**: `001-link-shortener`  
**Created**: 2026-11-03  
**Status**: Draft  
**Input**: User description: "Criar links curtos a partir de URLs longas, permitir códigos automáticos ou escolhidos, redirecionar e contar acessos, consultar o total e permitir expiração. Não inclui contas, domínio personalizado, relatório geográfico nem edição do destino."

## Clarifications

### Session 2026-11-03

- Q: Como o criador deve provar que pode consultar as estatísticas de um link, já que não haverá contas nem login? → A: Qualquer pessoa que conheça o código curto pode consultar as estatísticas.
- Q: Quais caracteres o criador deve poder usar em um código personalizado de 4 a 20 caracteres? → A: Letras, números, hífen e sublinhado.
- Q: Quantos acessos simultâneos a primeira versão deve suportar sem falhas? → A: A primeira versão não terá meta de capacidade simultânea.
- Q: A criação de links sem login deve ter um limite para reduzir spam e uso abusivo? → A: Não impor limite de criação na primeira versão.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Criar e compartilhar um link curto (Priority: P1)

O criador informa uma URL de destino e recebe um link curto. Pode aceitar um código gerado automaticamente ou escolher um código próprio, e pode opcionalmente informar quando o link deve expirar.

**Why this priority**: Criar um link compartilhável é o valor principal do produto.

**Independent Test**: Criar um link com uma URL válida e verificar que o código retornado pode ser compartilhado e usado para chegar ao destino.

**Acceptance Scenarios**:

1. **Given** uma URL HTTP ou HTTPS válida de até 2048 caracteres e nenhum código escolhido, **When** o criador solicita um link, **Then** recebe um código alfanumérico de exatamente 7 caracteres.
2. **Given** uma URL válida e um código escolhido de 4 a 20 caracteres composto por letras ASCII, números, hífen ou sublinhado e ainda não utilizado, **When** o criador solicita um link, **Then** recebe um link com esse código.
3. **Given** uma URL inválida, um código escolhido fora do tamanho permitido ou um código já utilizado, **When** o criador solicita um link, **Then** a solicitação é rejeitada com uma mensagem clara em português e nenhum link é criado.
4. **Given** uma data de expiração futura, **When** o criador solicita um link, **Then** o link é criado com essa data de expiração.

---

### User Story 2 - Acessar um link e contabilizar o acesso (Priority: P1)

Uma pessoa abre um link curto ativo e é encaminhada à URL de destino. Cada acesso a um link ativo é contabilizado, inclusive acessos repetidos pela mesma pessoa.

**Why this priority**: O redirecionamento é a função central do link curto; contar seus acessos permite medir seu uso.

**Independent Test**: Abrir um link ativo uma vez e confirmar o destino e o aumento do contador em um; repetir o acesso e confirmar novo aumento em um.

**Acceptance Scenarios**:

1. **Given** um link existente e ainda válido, **When** alguém o acessa, **Then** chega à URL de destino e o total de acessos aumenta em exatamente um.
2. **Given** um link existente e ainda válido, **When** alguém o acessa várias vezes, **Then** cada acesso aumenta o total em exatamente um.
3. **Given** um código inexistente, **When** alguém tenta acessá-lo, **Then** não é redirecionado a um destino e nenhum contador é alterado.

---

### User Story 3 - Consultar acessos e respeitar a expiração (Priority: P2)

O criador consulta o total de acessos de um link pelo seu código. Quando a data de expiração é atingida, o link deixa de redirecionar e novos acessos não são contabilizados.

**Why this priority**: A consulta do contador atende à necessidade de acompanhamento; a expiração permite limitar por quanto tempo o link funciona.

**Independent Test**: Consultar o total de um link e, em seguida, testar o comportamento antes e depois de sua expiração, verificando destino e contador.

**Acceptance Scenarios**:

1. **Given** um código existente, **When** alguém consulta suas estatísticas usando o código, **Then** recebe o total acumulado até aquele momento sem autenticação.
2. **Given** um link com expiração ainda no futuro, **When** alguém o acessa antes da expiração, **Then** é redirecionado e o acesso é contado.
3. **Given** um link expirado, **When** alguém o acessa, **Then** não é redirecionado e o total de acessos permanece inalterado.
4. **Given** um link criado anteriormente, **When** a aplicação é reiniciada, **Then** o link, seu destino, sua expiração e seu total continuam disponíveis e válidos conforme o estado anterior.

### Edge Cases

- Uma URL com exatamente 2048 caracteres é aceita; uma com mais de 2048 é rejeitada.
- URLs com esquema diferente de HTTP ou HTTPS, sem destino válido ou sem formato de URL absoluto são rejeitadas.
- Códigos escolhidos com menos de 4 ou mais de 20 caracteres são rejeitados.
- Códigos escolhidos contendo caracteres diferentes de letras ASCII, números, hífen e sublinhado são rejeitados.
- Códigos automáticos que colidirem com códigos existentes são gerados novamente até que se obtenha um código único.
- Uma expiração igual ao instante atual ou no passado é rejeitada na criação; no instante de expiração, o link já é considerado expirado.
- Acessos concorrentes ou repetidos não podem fazer o contador perder incrementos nem diminuir.
- Acessos após a expiração não alteram o contador, mesmo que o link tenha recebido acessos válidos anteriormente.
- Solicitações anônimas de criação de links não têm limite por origem na primeira versão.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir ao criador solicitar um link curto informando uma URL de destino e, opcionalmente, um código escolhido e uma data de expiração.
- **FR-002**: Quando o criador não informar um código, o sistema MUST gerar automaticamente um código com exatamente 7 caracteres alfanuméricos.
- **FR-003**: O sistema MUST aceitar códigos escolhidos com 4 a 20 caracteres formados apenas por letras ASCII, números, hífen ou sublinhado, e rejeitar códigos que não atendam a essas regras.
- **FR-004**: O sistema MUST garantir que cada código seja único em todo o serviço. Em caso de conflito com código escolhido, MUST rejeitar a criação; em caso de conflito com código automático, MUST gerar outro código.
- **FR-005**: O sistema MUST aceitar somente URLs absolutas com esquema HTTP ou HTTPS e no máximo 2048 caracteres.
- **FR-006**: Para cada acesso a um link existente e não expirado, o sistema MUST encaminhar a pessoa à URL de destino e incrementar o contador do link em exatamente um.
- **FR-007**: O sistema MUST tratar o link como expirado a partir do instante de expiração, não encaminhar acessos de links expirados e não incrementar o contador por esses acessos.
- **FR-008**: O sistema MUST permitir que qualquer pessoa consulte o total de acessos de um link pelo seu código, sem autenticação, e MUST manter esse total sem diminuição.
- **FR-009**: O sistema MUST preservar o link, destino, expiração e total de acessos após reinicializações da aplicação.
- **FR-010**: O sistema MUST rejeitar uma data de expiração igual ao momento da criação ou anterior a ele.
- **FR-011**: Ao rejeitar dados de criação, o sistema MUST informar o motivo de forma clara em português e não criar parcialmente o link.

### Key Entities

- **Link curto**: Representa a associação entre um código único, uma URL de destino, uma data de criação e uma data de expiração opcional.
- **Total de acessos**: Número acumulado de acessos contabilizados para um link; só aumenta em acessos válidos e é preservado junto ao link.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em 100% dos testes de criação automática, um link criado recebe um código com exatamente 7 caracteres alfanuméricos e sem colisão.
- **SC-002**: Em 100% dos testes de validação, URLs fora dos esquemas ou limites definidos e códigos escolhidos fora do tamanho ou conjunto de caracteres permitidos são rejeitados sem criar um link.
- **SC-003**: Em 100% dos testes com links ativos, cada acesso aumenta o total em exatamente um e encaminha ao destino correspondente.
- **SC-004**: Em 100% dos testes com links expirados, o acesso não encaminha ao destino e não altera o total.
- **SC-005**: Após uma reinicialização, 100% dos links de teste mantêm o mesmo destino, expiração e total de acessos.
- **SC-006**: Qualquer pessoa que conheça o código consegue consultar o total de acessos do link sem cadastro ou login.
- **SC-007**: A primeira versão não exige uma capacidade mínima garantida de acessos simultâneos.

## Assumptions

- A consulta pública revela apenas o total de acessos e não dados pessoais.
- Códigos escolhidos usam apenas letras ASCII, dígitos, hífen ou sublinhado; códigos diferenciam maiúsculas de minúsculas.
- A data de expiração representa um instante; quando informada com fuso horário, esse fuso determina o instante aplicável.
- Cada solicitação de acesso a um link ativo conta uma vez, sem tentar identificar pessoas únicas ou filtrar acessos automatizados.
- A primeira versão não tem uma meta mínima de capacidade simultânea.
- A criação anônima de links não terá limite de solicitações na primeira versão.
- A criação de links não inclui edição posterior do destino, conforme o escopo declarado.
- Cadastro e login de usuários, domínio personalizado e relatório geográfico estão fora do escopo.
