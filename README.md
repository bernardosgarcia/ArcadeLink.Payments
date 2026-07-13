# ArcadeLink.Payments

Microsserviço de pagamentos (processamento simulado de pagamentos) da arquitetura de microsserviços
orientada a eventos do FIAP Cloud Games (FCG) — Tech Challenge Fase 2.

Decidido como **stateless** (sem banco de dados) — não persiste histórico de transações.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/health` | Health check simples (útil para probes no k8s). |

## Configuração (appsettings)

- `RabbitMq:Host` / `RabbitMq:Username` / `RabbitMq:Password` — conexão com o broker.
- `Payments:ApprovalRate` — probabilidade (0.0–1.0) de um pagamento ser aprovado na simulação
  (padrão `0.8`, ou seja, 80% `Approved` / 20% `Rejected`, para demonstrar os dois caminhos).
  Pode ser forçado via variável de ambiente `Payments__ApprovalRate` para testes determinísticos.
- `Jwt:Issuer` / `Jwt:Audience` / `Jwt:Key` — precisam bater exatamente com os valores do
  `ArcadeLink.Users` (que emite o token). O pipeline de autenticação JWT está conectado, mas **nenhum
  endpoint HTTP aqui exige autorização hoje** — Payments é puramente orientado a eventos (só tem
  `GET /health`, público). A configuração existe para que um futuro endpoint autenticado só precise
  adicionar `.RequireAuthorization(...)`, sem precisar religar o pipeline.

## Consumer

- `OrderPlacedEventConsumer` → decide `Approved`/`Rejected` aleatoriamente conforme
  `Payments:ApprovalRate`, loga a decisão e publica `PaymentProcessedEvent`.

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
  `ArcadeLink.Catalog` e `ArcadeLink.Notifications` (`Status`: `"Approved"` | `"Rejected"`).
