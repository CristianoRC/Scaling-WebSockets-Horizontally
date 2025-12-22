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

## Arquitetura

```
Cliente → Nginx (LB) → Server 1 ↔ Redis ↔ Server 2
                     → Server 3 ↔ Redis
```

## Estrutura

```
├── ChatApi/
│   ├── Hubs/ChatHub.cs      # Hub SignalR
│   ├── wwwroot/index.html   # Frontend
│   ├── Program.cs           # Configuração
│   └── Dockerfile
├── docker-compose.yml
├── nginx.conf
└── README.md
```

## Demonstração

1. Abra várias abas em http://localhost:5000
2. Cada aba pode estar em um servidor diferente
3. Envie mensagens e veja a propagação em tempo real
4. As mensagens passam pelo Redis e chegam em todas as instâncias
