# ArcadeLink.Payments

Microsserviço de pagamentos (processamento simulado de pagamentos) da arquitetura de microsserviços
orientada a eventos do FIAP Cloud Games (FCG) — Tech Challenge Fase 2.

Decidido como **stateless** (sem banco de dados) — não persiste histórico de transações.

Este serviço **não é exposto pelo API Gateway (Kong)** — o edital só pede Gateway na frente de
Users e Catalog; Payments continua só orientado a eventos, sem tráfego HTTP externo (o único
endpoint HTTP que tem, `GET /health`, é só para health probes internos do k8s).

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/health` | Health check simples (útil para probes no k8s). |

## Como rodar localmente

Pré-requisito: .NET 8 SDK e RabbitMQ rodando (`docker start rabbitmq`) — Payments é stateless, não
precisa de Postgres.

```bash
dotnet restore
dotnet run
# ou, com hot reload:
dotnet watch run
```

Sobe em `http://localhost:5100`. Swagger em `/swagger`. Requisição de `GET /health` pronta em
`ArcadeLink.Payments.Api.http`.

## Configuração (variáveis de ambiente)

Os valores em `appsettings.json` são placeholders de desenvolvimento. Para sobrescrever (Docker,
compose, k8s), use a sintaxe de variável de ambiente do ASP.NET Core (`__` no lugar de `:`):

| Variável de ambiente | Descrição | Exemplo |
|---|---|---|
| `RabbitMq__Host` | Host do broker. | `localhost` (dev) / `rabbitmq` (compose/k8s) |
| `RabbitMq__Username` | Usuário do broker. | `guest` |
| `RabbitMq__Password` | Senha do broker. | `guest` |
| `Payments__ApprovalRate` | Probabilidade (0.0–1.0) de aprovar um pagamento na simulação. Pode ser forçado para `0`/`1` para testes determinísticos. | `0.8` (padrão: 80% `Approved` / 20% `Rejected`) |
| `Redis__ConnectionString` | Connection string do Redis (cache de `Payments:ApprovalRate`). | `localhost:6379` (dev) / `redis:6379` (k8s) |

## Docker

```bash
docker build -t arcadelink-payments .
```

A imagem final (`mcr.microsoft.com/dotnet/aspnet:8.0`) roda como usuário não-root e escuta na porta
**8080** (`ASPNETCORE_URLS=http://+:8080`) — não é possível bindar na porta 80 sem root. Em
k8s/compose, mapeie a porta externa (ex.: 80) para o `targetPort: 8080` do container.

Ao subir sozinho (fora do compose, contra a infra local exposta no host):

```bash
docker run -d --network host --name arcadelink-payments arcadelink-payments
```

**Orquestração completa**: o `docker-compose.yml` que sobe a aplicação inteira (Postgres, RabbitMQ e
os 4 serviços) mora no repo âncora, `ArcadeLink.Users` — não há um `docker-compose.yml` neste repo.
Rode a partir de lá com os 4 repositórios clonados lado a lado (mesmo diretório pai); veja o
[README do ArcadeLink.Users](../ArcadeLink.Users/README.md#orquestração-completa-docker-compose)
para instruções completas.

## Kubernetes

Manifestos em `k8s/` (`deployment.yaml`, `service.yaml`, `configmap.yaml`, `secret.yaml`) —
`Deployment` usa `image: localhost/arcadelink-payments:latest` com `imagePullPolicy: Never` (sem
registry, precisa ser carregada localmente no cluster antes do `kubectl apply`), `Service` expõe
`port: 80` → `targetPort: 8080`. `RabbitMq:Host`/`Username`/`Payments:ApprovalRate` em `ConfigMap`;
`RabbitMq:Password` em `Secret`.

O manifesto (`configmap.yaml`) já inclui `Redis__ConnectionString: "redis:6379"`, apontando para o
Service `redis` do `ArcadeLink.Orchestration` — nenhuma edição manual é necessária antes de aplicar.

Os manifestos de infraestrutura compartilhada (Postgres, RabbitMQ) ficam no repo âncora,
`ArcadeLink.Users/k8s/`; os de Redis ficam no `ArcadeLink.Orchestration` — veja o
[README do ArcadeLink.Users](../ArcadeLink.Users/README.md#kubernetes) e o
[README do ArcadeLink.Orchestration](../ArcadeLink.Orchestration/README.md) para o passo a passo
completo (build das imagens, carregar no cluster local, ordem de `kubectl apply`).

## Consumer

- `OrderPlacedEventConsumer` → decide `Approved`/`Rejected` aleatoriamente conforme
  `Payments:ApprovalRate`, loga a decisão e publica `PaymentProcessedEvent`. A partir da Fase 3,
  `Payments:ApprovalRate` não é mais lido direto de `IOptions<PaymentsOptions>` — o consumer depende
  de `IApprovalRateProvider`, implementado por `CachedApprovalRateProvider`
  (`Options/CachedApprovalRateProvider.cs`), que cacheia o valor no Redis com TTL de 30s (chave
  `config:approval-rate`) antes de cair no `IOptions<PaymentsOptions>` como fonte original.

## Testes

```bash
dotnet test
```

`tests/ArcadeLink.Payments.Api.Tests` (xUnit, 4 testes): os 2 originais — `OrderPlacedEventConsumerTests`,
usando `AddMassTransitTestHarness` (bus in-memory) — confirmam que `Payments:ApprovalRate = 1` sempre
publica `Approved` e `= 0` sempre publica `Rejected` — mais `CachedApprovalRateProviderTests`, novo na
Fase 3, cobrindo o cache-aside do Redis (hit retorna o valor cacheado sem tocar `IOptions`, miss lê de
`IOptions` e popula o cache). Não precisa de RabbitMQ/Redis rodando.

## Contratos de eventos

Este serviço **consome** `OrderPlacedEvent` e **publica** `PaymentProcessedEvent`.

Os contratos de eventos (namespace `ArcadeLink.Contracts`) **não são compartilhados via pacote
NuGet** — cada repositório mantém sua própria cópia idêntica (mesmo namespace, mesmo nome, mesmos
campos) em `src/ArcadeLink.Payments.Api/Contracts/`. Isso é intencional e simplifica o desafio; o
MassTransit resolve os exchanges do RabbitMQ pelo nome completo do tipo, então qualquer divergência
entre a cópia deste repo e a dos demais serviços quebra a integração silenciosamente.

Eventos definidos aqui:

- `OrderPlacedEvent(Guid OrderId, Guid UserId, Guid GameId, decimal Price)` — publicado por
  `ArcadeLink.Catalog`.
- `PaymentProcessedEvent(Guid OrderId, Guid UserId, Guid GameId, string Status)` — consumido por
  `ArcadeLink.Catalog` e pela `PurchaseConfirmationFunction` em `ArcadeLink.NotificationsFunction`
  (a partir da Fase 3; era `ArcadeLink.Notifications` na Fase 2) (`Status`: `"Approved"` |
  `"Rejected"`).
