# ArcadeLink.Payments

Microsserviço de pagamentos (processamento simulado de pagamentos) da arquitetura de microsserviços
orientada a eventos do FIAP Cloud Games (FCG) — Tech Challenge Fase 2.

Decidido como **stateless** (sem banco de dados) — não persiste histórico de transações.

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
