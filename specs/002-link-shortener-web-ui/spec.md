# Feature Specification: Interface web do Encurtador de links

**Feature Branch**: `002-link-shortener-web-ui`  
**Created**: 2026-11-04  
**Status**: Draft  
**Input**: User description: "Criar a interface web do Encurtador de links, consumindo a API da feature 001-link-shortener, sem alterar as regras de negócio nem os contratos backend. Inclui formulário de criação com cópia do link curto, consulta de acessos por código, exibição de erros em português junto aos campos e página clara para links inexistentes ou expirados."

## Clarifications

### Session 2026-11-04

- Q: Depois que um link curto é criado com sucesso, o que deve acontecer com os campos do formulário? → A: Exibir o link criado e limpar todos os campos do formulário.
- Q: Na consulta de acessos, o campo deve aceitar também o link curto completo colado pela pessoa, ou apenas o código? → A: Aceitar o código ou o link curto completo e extrair o código automaticamente.
- Q: Quanto tempo a interface espera pela API antes de exibir a mensagem geral de indisponibilidade? → A: 15 segundos.
- Q: Que comportamento a consulta deve ter quando o texto não é um código nem um link curto? → A: Validar na tela, sem chamar a API, com a mensagem "Informe um código ou um link curto válido." junto ao campo.
- Q: Qual padrão de acessibilidade a interface deve atender? → A: Apenas o descrito na FR-012 (teclado, rótulos e erros associados); sem conformidade WCAG formal.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Criar um link curto pela página (Priority: P1)

A pessoa acessa a página inicial, informa a URL de destino e, se quiser, um código personalizado e uma data e hora de expiração. Ao enviar, vê o link curto completo gerado, com um botão para copiá-lo e uma indicação de que foi copiado.

**Why this priority**: Criar e compartilhar um link é o valor principal; sem isso a interface não entrega utilidade.

**Independent Test**: Abrir a página, enviar uma URL válida e verificar que o link curto completo aparece e que o botão de copiar coloca esse link na área de transferência com confirmação visível.

**Acceptance Scenarios**:

1. **Given** a página inicial aberta, **When** a pessoa informa apenas uma URL válida e envia, **Then** vê o link curto completo gerado automaticamente.
2. **Given** a pessoa informou URL, código personalizado válido e expiração futura, **When** envia, **Then** vê o link curto completo com o código escolhido.
3. **Given** um link curto exibido, **When** a pessoa aciona o botão de copiar, **Then** o link completo é copiado e a página indica claramente que foi copiado.
4. **Given** um link criado com sucesso, **When** o resultado é exibido, **Then** todos os campos do formulário ficam vazios e o link criado permanece visível.
5. **Given** um link já criado e exibido, **When** a pessoa decide criar outro, **Then** consegue enviar uma nova solicitação sem recarregar manualmente a página.

---

### User Story 2 - Entender e corrigir dados rejeitados (Priority: P1)

Quando os dados são rejeitados, a página mostra o motivo em português junto ao campo correspondente e mantém tudo o que a pessoa digitou, para que ela corrija apenas o necessário.

**Why this priority**: Sem mensagens claras e sem perda de dados, a criação de links vira tentativa e erro e frustra o uso.

**Independent Test**: Enviar, em testes separados, uma URL inválida, um código fora das regras, um código já usado e uma expiração no passado, verificando a mensagem junto ao campo certo e os valores preservados.

**Acceptance Scenarios**:

1. **Given** uma URL inválida, **When** a pessoa envia, **Then** o motivo aparece em português junto ao campo da URL e nenhum link é exibido.
2. **Given** um código fora das regras ou já usado, **When** a pessoa envia, **Then** o motivo aparece em português junto ao campo do código.
3. **Given** uma expiração no passado, **When** a pessoa envia, **Then** o motivo aparece em português junto ao campo de expiração.
4. **Given** qualquer rejeição, **When** a mensagem é exibida, **Then** todos os campos mantêm os valores digitados.
5. **Given** o serviço temporariamente indisponível ou sem resposta, **When** a pessoa envia, **Then** vê uma mensagem geral em português, com os campos preservados, e pode tentar novamente.

---

### User Story 3 - Consultar o total de acessos (Priority: P2)

A pessoa informa o código curto de um link e vê o total de acessos acumulado, sem login.

**Why this priority**: Atende à necessidade de acompanhamento, mas depende de já existirem links criados.

**Independent Test**: Informar o código de um link existente e conferir que o total exibido corresponde ao informado pelo serviço; informar um código inexistente e conferir a mensagem.

**Acceptance Scenarios**:

1. **Given** o código ou o link curto completo de um link existente, **When** a pessoa consulta, **Then** vê o total de acessos desse link.
2. **Given** um código inexistente, **When** a pessoa consulta, **Then** vê uma mensagem em português informando que nenhum link foi encontrado para esse código, junto ao campo do código. (A consulta de acessos da feature 001 não considera expiração: um link expirado continua exibindo o total acumulado.)
3. **Given** uma consulta com erro, **When** a mensagem é exibida, **Then** o código digitado é mantido.
4. **Given** um link recém-criado na mesma página, **When** a pessoa quer consultar seus acessos, **Then** pode fazê-lo informando o código exibido.

---

### User Story 4 - Página para link inexistente ou expirado (Priority: P2)

Quando alguém abre no navegador um link curto que não existe ou expirou, vê uma página clara em português explicando isso, com uma opção para criar um novo link.

**Why this priority**: Evita que quem clica em um link inválido veja uma resposta técnica incompreensível.

**Independent Test**: Abrir no navegador um código inexistente e um link expirado e verificar a página em português com a opção de criar um novo link, que leva ao formulário de criação.

**Acceptance Scenarios**:

1. **Given** um código curto inexistente, **When** alguém o abre no navegador, **Then** vê uma página em português informando que o link não existe ou expirou.
2. **Given** um link expirado, **When** alguém o abre no navegador, **Then** vê a mesma página informativa, sem ser redirecionado.
3. **Given** a página de link inexistente ou expirado, **When** a pessoa aciona a opção de criar um novo link, **Then** é levada ao formulário de criação.
4. **Given** um link válido, **When** alguém o abre no navegador, **Then** continua sendo redirecionado ao destino, conforme a feature 001.

### Edge Cases

- Campos de código e expiração em branco são tratados como não informados; o link é criado com código automático e sem expiração.
- A URL e o código digitados são comparados exatamente como informados; espaços acidentais nas extremidades não devem produzir erro confuso para o usuário.
- Se o navegador não permitir a cópia automática, a página informa que não foi possível copiar e mantém o link visível para cópia manual.
- Um segundo envio feito enquanto o primeiro ainda está em andamento não cria links duplicados.
- Um erro ao consultar acessos não afeta o link exibido nem o conteúdo do formulário de criação.
- Uma data e hora de expiração é interpretada no fuso horário local da pessoa e exibida de forma coerente com o que ela informou.
- Mensagens da etapa anterior (erro ou link criado) não permanecem visíveis de forma enganosa depois de um novo envio.
- Um link curto completo colado na consulta de acessos é reduzido ao seu código; um texto que não seja código nem link curto é rejeitado na própria página, sem chamar a API, com a mensagem "Informe um código ou um link curto válido." junto ao campo, sem travar a página.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A interface MUST oferecer uma página inicial com um formulário contendo URL de destino (obrigatória), código personalizado (opcional) e data e hora de expiração (opcional).
- **FR-002**: Ao receber um link criado com sucesso, a interface MUST exibir o link curto completo, pronto para ser compartilhado, e MUST limpar todos os campos do formulário mantendo o link exibido.
- **FR-003**: A interface MUST oferecer um botão para copiar o link curto completo e MUST indicar claramente quando a cópia foi feita ou quando não foi possível realizá-la.
- **FR-004**: A interface MUST permitir consultar o total de acessos de um link informando seu código curto ou o link curto completo, extraindo o código automaticamente, sem login.
- **FR-005**: Quando a criação for rejeitada, a interface MUST exibir o motivo em português junto ao campo correspondente (URL, código ou expiração), sem apagar nenhum valor digitado.
- **FR-006**: Quando a consulta de acessos não encontrar o link, a interface MUST exibir em português que nenhum link foi encontrado para o código, mantendo o código digitado. Links expirados existentes continuam exibindo o total de acessos, conforme a API da feature 001.
- **FR-007**: Quando a rejeição não puder ser associada a um campo específico, ou o serviço estiver indisponível, a interface MUST exibir uma mensagem geral em português e permitir nova tentativa sem perder os dados. Uma chamada à API sem resposta em 15 segundos MUST ser tratada como serviço indisponível.
- **FR-008**: Ao abrir no navegador um link curto inexistente ou expirado, a interface MUST exibir uma página em português informando que o link não existe ou expirou, com opção de criar um novo link.
- **FR-009**: A interface MUST NOT alterar as regras de negócio, os contratos nem o comportamento do serviço da feature 001; MUST apenas consumi-los.
- **FR-010**: A interface MUST apresentar o estado de envio enquanto uma solicitação está em andamento e MUST impedir envios duplicados simultâneos.
- **FR-011**: Todos os textos, rótulos e mensagens exibidos à pessoa MUST estar em português.
- **FR-012**: A interface MUST ser utilizável por teclado e em telas de celular e de computador, com campos identificados por rótulos e mensagens de erro associadas aos campos. Não há exigência de conformidade formal com WCAG nesta versão.

### Key Entities

- **Solicitação de criação**: Dados digitados pela pessoa: URL de destino, código personalizado opcional e expiração opcional.
- **Link curto exibido**: Resultado da criação, com o link curto completo, o código e a expiração quando houver.
- **Consulta de acessos**: Código informado pela pessoa e o total de acessos retornado, ou a mensagem de link não encontrado.
- **Mensagem de erro**: Texto em português associado a um campo específico ou ao formulário como um todo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Uma pessoa consegue criar um link curto e vê o resultado em até 1 minuto, sem instruções, a partir da abertura da página.
- **SC-002**: Em 100% dos testes de rejeição (URL inválida, código fora das regras, código já usado e expiração no passado), a mensagem aparece em português junto ao campo correto e 100% dos valores digitados são preservados.
- **SC-003**: Em 100% dos testes de cópia, o link copiado é idêntico ao link curto exibido, e a indicação de sucesso ou falha aparece em até 1 segundo.
- **SC-004**: A pessoa consegue consultar o total de acessos de um link em até 3 interações após abrir a página: digitar o código, acionar a consulta e ler o resultado.
- **SC-005**: Em 100% dos testes com códigos inexistentes ou links expirados abertos no navegador, a pessoa vê a página em português com a opção de criar novo link, e nenhum redirecionamento ocorre.
- **SC-006**: Todos os fluxos principais funcionam em tela de celular (320 px de largura) e de computador, sem rolagem horizontal e usando apenas o teclado.
- **SC-007**: Nenhum teste existente das regras e contratos da feature 001 muda de resultado após a entrega desta interface.

## Assumptions

- A interface é pública e anônima, sem contas, login, histórico ou lista de links de quem usa a interface.
- A interface usa os mesmos limites de URL, código e expiração já definidos pela feature 001; não os redefine.
- O serviço da feature 001 devolve um único motivo de rejeição por solicitação; a interface associa esse motivo ao campo correspondente quando isso for possível pelo conteúdo da mensagem e, caso contrário, exibe-o como mensagem geral.
- A página para links inexistentes ou expirados deve ser apresentada a pessoas que abrem links curtos no navegador, sem mudar o comportamento de redirecionamento nem as respostas existentes do serviço para consumidores da API.
- Esta feature não inclui cadastro e login, domínio personalizado, relatório geográfico, edição do destino, histórico ou lista de links e qualquer limite de criação.
- A interface é voltada a navegadores atuais de computador e celular; navegadores muito antigos estão fora do escopo.
- O link curto completo usa o endereço público já fornecido pelo serviço na criação.
