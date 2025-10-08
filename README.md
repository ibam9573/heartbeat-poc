# Heartbeat POC em .NET 8: Monitoramento de Processos Distribuídos

## Visão Geral do Projeto

Esta Prova de Conceito (POC) em ASP.NET Core 8 (Minimal API) demonstra um sistema de monitoramento de processos baseado em "heartbeats" (batimentos cardíacos). O objetivo é simular e gerenciar o ciclo de vida de processos de curta e longa duração em um ambiente distribuído, onde a API atua como um ponto central para registrar, monitorar e logar a atividade desses componentes.

O cenário principal é o de sistemas que executam tarefas assíncronas ou em background (ex: importação de dados, processamento de filas), onde a visibilidade do status de cada tarefa é crucial para a operação e a detecção proativa de falhas.

## Arquitetura e Design

A arquitetura desta POC foi concebida para ser **simples, funcional e demonstrar boas práticas de desenvolvimento .NET**, mesmo em um escopo minimalista.

### Tecnologias Utilizadas

*   **.NET 8:** Framework principal para o desenvolvimento da API.
*   **ASP.NET Core Minimal APIs:** Abordagem moderna para construir APIs leves e de alta performance.
*   **BackgroundService:** Para execução de tarefas em segundo plano de forma assíncrona e periódica.
*   **ConcurrentDictionary<TKey, TValue>:** Coleção thread-safe para gerenciamento de estado em memória.
*   **Dependency Injection (DI):** Para gerenciamento de dependências e inversão de controle.
*   **Microsoft.Extensions.Logging:** Para um sistema de logging robusto e configurável.
*   **Swagger/OpenAPI:** Para documentação e teste interativo da API.

### Diagrama de Arquitetura (Visão Lógica)

```
+---------------------+       +---------------------+
|   HeartbeatPOC API  |       |   HeartbeatPOC API  |
| (ASP.NET Core 8)    |       | (ASP.NET Core 8)    |
|                     |       |                     |
|  +----------------+ |       |  +----------------+ |
|  | HeartbeatRegistry| <-----> | ProcessMonitor   | |
|  | (Singleton)      | |       | (BackgroundService)| |
|  +----------------+ |       |  +----------------+ |
|          ^          |       |          ^          |
|          |          |       |          |          |
|  +----------------+ |       |  +----------------+ |
|  | API Endpoints    | <-----> | Console/Logs     | |
|  | (POST, GET, DEL) | |       | (Active Processes)| |
|  +----------------+ |       |  +----------------+ |
+----------|----------+       +----------|----------+
           |                             |
           | (HTTP Requests)             | (Internal Calls)
           V                             V
+-----------------------------------------------------+
|                 External Clients / Tools            |
| (Insomnia/Postman para criar/remover processos)     |
+-----------------------------------------------------+
```

### Componentes Principais:

*   **API ASP.NET Core Minimal:** A base da aplicação, expondo endpoints RESTful para interação. A escolha da Minimal API reflete uma abordagem moderna para construir APIs leves e de alta performance no .NET, ideal para microsserviços e POCs.
*   **`Models/ProcessStatus.cs`:** Define o contrato de dados para o status de um processo. Inclui `ProcessId` (identificador único), `LastHeartbeat` (timestamp do último batimento), `IsAlive` (propriedade computada que indica se o processo está ativo nos últimos 30 segundos) e `KeepAlive` (uma flag que determina se o `ProcessMonitorWorker` deve manter este processo vivo).
*   **`Services/HeartbeatRegistry.cs`:** Um serviço central (`singleton`) que gerencia o estado de todos os processos monitorados.
    *   Utiliza um `ConcurrentDictionary<string, ProcessStatus>` para armazenamento em memória, garantindo operações thread-safe em um ambiente concorrente.
    *   **`CreateProcess(bool keepAlive)`:** Inicia um novo processo lógico, gerando um `ProcessId` e definindo sua política de `KeepAlive`.
    *   **`RegisterHeartbeat(string processId)`:** Atualiza o `LastHeartbeat` de um processo existente.
    *   **`GetStatuses()` / `GetActiveStatuses()`:** Métodos para consultar o estado de todos os processos ou apenas dos que estão `IsAlive`.
    *   **`RemoveHeartbeat(string processId)`:** Remove um processo do registro, simulando seu encerramento.
*   **`Services/ProcessMonitorWorker.cs`:** Implementado como um `BackgroundService`, este worker opera em segundo plano, de forma assíncrona e periódica (a cada 5 segundos).
    *   Sua função é simular a atividade de processos de longa duração (`KeepAlive = true`) chamando `HeartbeatRegistry.RegisterHeartbeat` para eles.
    *   **Loga no console apenas os `ProcessId` dos processos que estão `IsAlive = true`**, fornecendo uma visão em tempo real do estado do sistema. Se não houver processos ativos, ele loga uma mensagem indicando isso, mantendo o output limpo e relevante.

### Fluxo de Operação (Ciclo de Vida de um Heartbeat)

Para entender o fluxo completo, considere os seguintes cenários:

#### 1. Criação de um Processo (Short-Lived ou Long-Lived)

1.  Um cliente externo (e.g., Insomnia) envia uma requisição `POST` para `/process/short-lived` ou `/process/long-lived`.
2.  A API recebe a requisição e, através da injeção de dependência, acessa a instância `singleton` de `HeartbeatRegistry`.
3.  `HeartbeatRegistry.CreateProcess(bool keepAlive)` é chamado:
    *   Um `ProcessId` único é gerado (e.g., `ShortLivedProcess_XXXX`).
    *   Um novo objeto `ProcessStatus` é criado com `ProcessId`, `LastHeartbeat` (tempo atual) e a flag `KeepAlive` (baseada na requisição).
    *   Este `ProcessStatus` é adicionado ao `ConcurrentDictionary` interno do `HeartbeatRegistry`.
    *   Um log informativo é gerado.
4.  A API retorna o `ProcessId` e uma mensagem de sucesso ao cliente.

#### 2. Monitoramento e Manutenção de Heartbeats (para processos Long-Lived)

1.  O `ProcessMonitorWorker`, que é um `BackgroundService`, executa seu método `ExecuteAsync` periodicamente (a cada 5 segundos).
2.  Dentro de `ExecuteAsync`, ele consulta o `HeartbeatRegistry` para obter todos os processos registrados.
3.  Para cada `ProcessStatus` onde `IsAlive` é `true`:
    *   O `ProcessMonitorWorker` loga os detalhes do processo no console.
    *   Se a flag `KeepAlive` do processo for `true` (indicando um processo de longa duração), o `ProcessMonitorWorker` chama `HeartbeatRegistry.RegisterHeartbeat(process.ProcessId)`.
    *   `HeartbeatRegistry.RegisterHeartbeat` atualiza o `LastHeartbeat` do processo no `ConcurrentDictionary` para o tempo atual, efetivamente "mantendo-o vivo".
4.  Se não houver processos ativos, uma mensagem indicando isso é logada.
5.  O worker aguarda 5 segundos antes da próxima iteração.

#### 3. Expiração de Processos (Short-Lived)

1.  Um processo de curta duração é criado (vide "Criação de um Processo"). Sua flag `KeepAlive` é `false`.
2.  O `ProcessMonitorWorker` o loga como ativo inicialmente.
3.  Como `KeepAlive` é `false`, o `ProcessMonitorWorker` *não* chama `HeartbeatRegistry.RegisterHeartbeat` para este processo.
4.  Após 30 segundos (definido na propriedade `IsAlive` de `ProcessStatus`), a diferença entre `DateTime.UtcNow` e `LastHeartbeat` excede o limite.
5.  A propriedade `IsAlive` do `ProcessStatus` para este processo se torna `false`.
6.  Nas iterações subsequentes, o `ProcessMonitorWorker` e os endpoints `GET /heartbeat/active-status` não incluirão mais este processo, pois ele não é mais considerado "ativo".

#### 4. Encerramento Explícito de um Processo

1.  Um cliente envia uma requisição `DELETE` para `/heartbeat/{processId}`.
2.  A API recebe a requisição e acessa o `HeartbeatRegistry`.
3.  `HeartbeatRegistry.RemoveHeartbeat(processId)` é chamado.
4.  O processo correspondente é removido do `ConcurrentDictionary`.
5.  A API retorna uma mensagem de sucesso.
6.  Nas próximas iterações do `ProcessMonitorWorker` e consultas aos endpoints de status, o processo não será mais listado.

## Problemas Resolvidos (Desafios, Soluções e Benefícios)

Esta POC aborda e resolve os seguintes desafios comuns em sistemas distribuídos, demonstrando uma abordagem robusta e eficiente:

1.  **Desafio: Visibilidade e Gerenciamento de Processos de Curta Duração.**
    *   **Solução:** Processos são registrados com um `LastHeartbeat` inicial e uma flag `KeepAlive=false`. A propriedade computada `IsAlive` em `ProcessStatus` determina a expiração automática após um período sem atualização.
    *   **Benefício:** Permite o monitoramento de tarefas rápidas que não necessitam de manutenção contínua, liberando recursos automaticamente após sua conclusão ou inatividade.

2.  **Desafio: Manutenção e Monitoramento de Processos de Longa Duração.**
    *   **Solução:** Processos são registrados com `KeepAlive=true`. O `ProcessMonitorWorker` (um `BackgroundService`) periodicamente atualiza o `LastHeartbeat` desses processos, simulando sua atividade contínua.
    *   **Benefício:** Garante que tarefas contínuas ou de longa execução sejam ativamente monitoradas e mantidas como "vivas" no sistema, fornecendo um status em tempo real.

3.  **Desafio: Encerramento Controlado e Simulação de Falhas de Processos.**
    *   **Solução:** Um endpoint `DELETE /heartbeat/{processId}` permite a remoção explícita de um processo do registro.
    *   **Benefício:** Facilita a simulação de encerramentos limpos ou a remoção de processos que falharam e não estão mais enviando heartbeats, mantendo o estado do sistema consistente.

4.  **Desafio: Redução de Ruído e Foco na Informação Relevante.**
    *   **Solução:** Tanto o `ProcessMonitorWorker` quanto o endpoint `GET /heartbeat/active-status` filtram e exibem apenas os processos que estão `IsAlive=true`.
    *   **Benefício:** Proporciona uma visão clara e concisa do estado operacional do sistema, evitando a poluição do log e da interface de consulta com informações de processos inativos ou expirados.

5.  **Desafio: Modelagem de um Sistema de Importação/Processamento Assíncrono.**
    *   **Solução:** A API atua como um ponto de controle central para iniciar e monitorar "tarefas" (processos), refletindo o ciclo de vida de operações assíncronas como importações de dados.
    *   **Benefício:** Demonstra um padrão aplicável a cenários reais onde a visibilidade do progresso e status de tarefas em background é fundamental para a operação e auditoria.

## Como Rodar a POC

### Pré-requisitos:

*   .NET 8 SDK instalado.
*   Um cliente HTTP como Insomnia, Postman ou `Invoke-WebRequest` (PowerShell) / `curl` (WSL/Git Bash) para interagir com a API.

### Passos:

1.  **Navegue até o diretório da POC:**
    ```bash
    cd HeartbeatPOC
    ```
2.  **Inicie a API:**
    ```bash
    dotnet watch run
    ```
    A API será iniciada (geralmente em `http://localhost:5277`). O `ProcessMonitorWorker` começará a logar no console a cada 5 segundos.

3.  **Importe a Coleção do Insomnia (Opcional, mas recomendado):**
    O arquivo `heartbeat-insomnia.json` na raiz do projeto contém uma coleção pré-configurada para todos os endpoints. Importe-o no seu cliente HTTP favorito.

### Endpoints da API:

*   **`POST /process/short-lived`**
    *   **Descrição:** Cria um novo processo de curta duração.
    *   **Retorna:** `{ "processId": "ShortLivedProcess_XXXX", "message": "Short-lived process ... created." }`
*   **`POST /process/long-lived`**
    *   **Descrição:** Cria um novo processo de longa duração.
    *   **Retorna:** `{ "processId": "LongLivedProcess_YYYY", "message": "Long-lived process ... created." }`
*   **`GET /heartbeat/status`**
    *   **Descrição:** Retorna o status de *todos* os processos registrados (ativos e inativos).
*   **`GET /heartbeat/active-status`**
    *   **Descrição:** Retorna o status *apenas dos processos ativos* (`IsAlive: true`).
*   **`DELETE /heartbeat/{processId}`**
    *   **Descrição:** Remove um processo específico do registro. Substitua `{processId}` pelo ID real do processo.
    *   **Exemplo:** `DELETE http://localhost:5277/heartbeat/LongLivedProcess_YYYY`

## Demonstração do Comportamento

Siga estes passos para observar o comportamento da POC:

1.  **Inicie a API** (`dotnet watch run`). Observe os logs do `ProcessMonitorWorker` indicando "No active processes to log."
2.  **Crie um Processo de Curta Duração:**
    *   Faça um `POST` para `http://localhost:5277/process/short-lived`. Anote o `ProcessId` retornado.
    *   Observe o terminal da API: o `ProcessMonitorWorker` começará a logar este processo como ativo.
    *   Após aproximadamente 30 segundos, o processo desaparecerá dos logs ativos do `ProcessMonitorWorker` e do endpoint `GET /heartbeat/active-status`, pois seu `LastHeartbeat` não foi atualizado.
3.  **Crie um Processo de Longa Duração:**
    *   Faça um `POST` para `http://localhost:5277/process/long-lived`. Anote o `ProcessId` retornado.
    *   Observe o terminal da API: o `ProcessMonitorWorker` começará a logar este processo como ativo e o manterá assim, pois `KeepAlive` é `true`.
    *   Consulte `GET /heartbeat/active-status` para ver que ele permanece ativo.
4.  **Simule o Encerramento de um Processo de Longa Duração:**
    *   Faça um `DELETE` para `http://localhost:5277/heartbeat/{ProcessId}` (substitua pelo ID do seu processo de longa duração).
    *   Observe o terminal da API: o processo de longa duração desaparecerá dos logs ativos do `ProcessMonitorWorker` e do endpoint `GET /heartbeat/active-status`.

## Extensões Futuras (Sugestões para um Ambiente de Produção)

Para evoluir esta Prova de Conceito para um sistema robusto e pronto para produção, as seguintes extensões são recomendadas, demonstrando um planejamento de arquitetura escalável e resiliente:

*   **Persistência de Dados e Escalabilidade:**
    *   **Detalhes:** Substituir o `ConcurrentDictionary` em memória por um mecanismo de persistência de dados.
    *   **Opções:**
        *   **Banco de Dados Relacional (e.g., SQL Server, PostgreSQL):** Para armazenamento transacional e consultas complexas.
        *   **NoSQL (e.g., Cosmos DB, MongoDB):** Para alta escalabilidade e flexibilidade de esquema, ideal para logs e estados de processos.
        *   **Cache Distribuído (e.g., Redis):** Para estados de processos de alta volatilidade e baixa latência, permitindo que múltiplas instâncias da API compartilhem o mesmo estado.
    *   **Benefício:** Garante a durabilidade dos dados, permite a recuperação de estado após reinícios da aplicação e suporta a escalabilidade horizontal da API.

*   **Mecanismo de Heartbeat Distribuído e Autônomo:**
    *   **Detalhes:** Em vez de o `ProcessMonitorWorker` simular heartbeats para processos `KeepAlive`, os próprios processos externos (clientes) seriam responsáveis por enviar seus batimentos para a API.
    *   **Implementação:** Os clientes implementariam uma lógica para chamar `POST /heartbeat/{processId}/register` (um novo endpoint) periodicamente.
    *   **Benefício:** Descentraliza a responsabilidade de manutenção do heartbeat, tornando o sistema mais fiel a um ambiente distribuído real e reduzindo a carga sobre a API central.

*   **Sistema de Notificações Proativas:**
    *   **Detalhes:** Implementar um mecanismo para alertar equipes de operação quando um processo de longa duração se torna inativo inesperadamente (e.g., falha ou travamento).
    *   **Integrações:**
        *   **E-mail (e.g., SendGrid, Mailgun):** Para alertas formais.
        *   **SMS (e.g., Twilio):** Para alertas críticos.
        *   **Ferramentas de Colaboração (e.g., Slack, Microsoft Teams):** Para integração com fluxos de trabalho de equipes.
    *   **Benefício:** Permite a detecção proativa de problemas, minimizando o tempo de inatividade e melhorando a confiabilidade do sistema.

*   **Interface de Usuário (UI) de Monitoramento:**
    *   **Detalhes:** Desenvolver uma interface web simples para visualizar o status dos processos em tempo real, com filtros e capacidades de busca.
    *   **Tecnologias:** React, Angular, Vue.js para o frontend, consumindo os endpoints da API.
    *   **Benefício:** Oferece uma ferramenta visual para operadores e desenvolvedores acompanharem o estado dos processos sem a necessidade de ferramentas de linha de comando ou clientes HTTP.

*   **Configuração Externa e Flexibilidade:**
    *   **Detalhes:** Externalizar parâmetros chave como o intervalo do `ProcessMonitorWorker` e o tempo de expiração de `IsAlive` para o `appsettings.json`.
    *   **Implementação:** Utilizar o sistema de configuração do .NET Core (`IConfiguration`) para carregar esses valores.
    *   **Benefício:** Aumenta a flexibilidade do sistema, permitindo ajustes de comportamento sem recompilação e facilitando a adaptação a diferentes ambientes (desenvolvimento, produção).

*   **Autenticação e Autorização:**
    *   **Detalhes:** Proteger os endpoints da API para garantir que apenas clientes autorizados possam criar, registrar ou remover processos.
    *   **Implementação:** Integrar com JWT (JSON Web Tokens) ou OAuth2.
    *   **Benefício:** Aumenta a segurança da API, prevenindo acessos não autorizados e garantindo a integridade dos dados de monitoramento.
