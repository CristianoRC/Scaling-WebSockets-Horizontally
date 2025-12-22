# SignalR + Redis - Escala Horizontal

Demonstração de escala horizontal com WebSockets usando SignalR e Redis como backplane.

## Tecnologias

- .NET 10
- SignalR
- Redis (Pub/Sub)
- Nginx (Load Balancer)
- Docker Compose

## Como Executar

```bash
docker-compose up --build
```

Acesse: http://localhost:5000

## Dois Modos de Demonstração

### 🪄 Modo Automático (`/chatHub`)
Usa `AddStackExchangeRedis()` - o SignalR cuida de tudo.

```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString);
```

### 🔧 Modo Manual (`/manualChatHub`)  
Implementação explícita do Redis Pub/Sub.

```csharp
// Publicar
await subscriber.PublishAsync(channel, message);

// Assinar
await subscriber.SubscribeAsync(channel, (ch, msg) => { ... });
```

## Arquitetura

```
Cliente → Nginx (LB) → Server-X → Redis PUBLISH
                           │
              ┌────────────┼────────────┐
              ▼            ▼            ▼
          Server-1     Server-2     Server-3
          (SUBSCRIBE)  (SUBSCRIBE)  (SUBSCRIBE)
              │            │            │
              ▼            ▼            ▼
          Clientes     Clientes     Clientes
```

## Estrutura

```
├── ChatApi/
│   ├── Hubs/
│   │   ├── ChatHub.cs        # Hub automático
│   │   └── ManualChatHub.cs  # Hub manual
│   ├── Services/
│   │   ├── RedisPublisher.cs # Publica no Redis
│   │   └── RedisSubscriber.cs# Assina o Redis
│   ├── wwwroot/index.html    # Frontend
│   ├── Program.cs
│   └── Dockerfile
├── docker-compose.yml
├── nginx.conf
└── README.md
```

## Demonstração

1. Abra várias abas em http://localhost:5000
2. Alterne entre modo Automático e Manual
3. Envie mensagens e veja a propagação via Redis
4. Observe os logs: `docker-compose logs -f`
