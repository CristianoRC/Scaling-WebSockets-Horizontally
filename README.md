# 🚀 Escala Horizontal com WebSockets

## O Problema

Imagine que você tem um chat funcionando em **um servidor**. Tudo funciona bem.

Mas e quando você precisa de **mais servidores** para aguentar mais usuários?

```
Usuário A conecta no Server-1
Usuário B conecta no Server-2

A envia mensagem... B não recebe! 😱
```

**Por quê?** Porque cada servidor só conhece seus próprios clientes.

---

## A Solução: Redis como "Ponte"

O Redis funciona como um **mensageiro central** entre os servidores.

1. **Server-1** recebe mensagem do Usuário A
2. **Server-1** publica no Redis
3. **Redis** avisa todos os servidores
4. **Server-2** recebe e envia pro Usuário B

Agora todos recebem todas as mensagens! ✅

---

## Como Rodar

```bash
docker-compose up --build
```

Acesse: **http://localhost:8080**

---

## Arquitetura C4 Model

### Nível 1: Contexto do Sistema

```mermaid
C4Context
    title Sistema de Chat - Diagrama de Contexto

    Person(user, "Usuário", "Pessoa que usa o chat em tempo real")
    
    System(chatSystem, "Sistema de Chat", "Permite enviar e receber mensagens em tempo real com escala horizontal")

    Rel(user, chatSystem, "Envia e recebe mensagens", "HTTP/WebSocket")
```

---

### Nível 2: Diagrama de Containers

```mermaid
C4Container
    title Sistema de Chat - Diagrama de Containers

    Person(user, "Usuário", "Pessoa que usa o chat")

    Container_Boundary(system, "Sistema de Chat") {
        Container(nginx, "NGINX", "Load Balancer", "Serve frontend estático e distribui conexões WebSocket via ip_hash")
        
        Container(server1, "Server-1", ".NET 10 / SignalR", "Instância 1 da API de chat")
        Container(server2, "Server-2", ".NET 10 / SignalR", "Instância 2 da API de chat")
        Container(server3, "Server-3", ".NET 10 / SignalR", "Instância 3 da API de chat")
        
        ContainerDb(redis, "Redis", "Message Broker", "Pub/Sub backplane para propagar mensagens entre instâncias")
    }

    Rel(user, nginx, "Acessa", "HTTPS/WSS :8080")
    Rel(nginx, server1, "Roteia", "HTTP/WS")
    Rel(nginx, server2, "Roteia", "HTTP/WS")
    Rel(nginx, server3, "Roteia", "HTTP/WS")
    Rel(server1, redis, "Pub/Sub", "TCP :6379")
    Rel(server2, redis, "Pub/Sub", "TCP :6379")
    Rel(server3, redis, "Pub/Sub", "TCP :6379")
```

---

### Fluxo de Mensagem

```mermaid
sequenceDiagram
    participant A as 👤 User A
    participant N as 🌐 NGINX
    participant S1 as 🟢 Server-1
    participant R as 🔴 Redis
    participant S2 as 🟣 Server-2
    participant B as 👤 User B

    A->>N: Envia "Olá!" (WebSocket)
    N->>S1: Roteia (ip_hash)
    S1->>R: PUBLISH chat:messages
    R-->>S1: (recebe própria msg)
    R-->>S2: SUBSCRIBE notifica
    S2->>B: Envia "Olá!" (WebSocket)
    
    Note over A,B: Mensagem propagada em tempo real! ✅
```

---

### Visão Geral da Infraestrutura

```mermaid
flowchart TB
    subgraph Internet
        U1[👤 Usuário 1]
        U2[👤 Usuário 2]
        U3[👤 Usuário 3]
    end

    subgraph Docker["🐳 Docker Network"]
        N[🌐 NGINX<br/>Load Balancer<br/>:8080]
        
        subgraph Servers["API Servers"]
            S1[🟢 Server-1<br/>.NET SignalR]
            S2[🟣 Server-2<br/>.NET SignalR]
            S3[🟠 Server-3<br/>.NET SignalR]
        end
        
        R[(🔴 Redis<br/>Pub/Sub<br/>:6379)]
    end

    U1 & U2 & U3 --> N
    N --> S1 & S2 & S3
    S1 & S2 & S3 <--> R

    style N fill:#4ade80,stroke:#22c55e,color:#000
    style R fill:#f87171,stroke:#ef4444,color:#000
    style S1 fill:#2dd4bf,stroke:#14b8a6,color:#000
    style S2 fill:#c084fc,stroke:#a855f7,color:#000
    style S3 fill:#fbbf24,stroke:#f59e0b,color:#000
```

---

## O Que Está Rodando

| Container | Tipo | Função |
|-----------|------|--------|
| **nginx** | Load Balancer | Serve frontend + distribui conexões |
| **server-1** | API | Instância 1 do SignalR |
| **server-2** | API | Instância 2 do SignalR |
| **server-3** | API | Instância 3 do SignalR |
| **redis** | Message Broker | Pub/Sub entre instâncias |

---

## Teste Você Mesmo

1. Abra **3 abas** do navegador em http://localhost:8080
2. Veja que cada aba pode conectar em um **servidor diferente**
3. Envie uma mensagem em qualquer aba
4. **Todas as abas recebem!** 🎉

---

## Dois Modos de Implementação

O modo é configurado via variável de ambiente `Redis__Mode` no `docker-compose.yml`.

### 🪄 Automático (`Redis__Mode: SignalR`)
O SignalR faz tudo sozinho. Você só adiciona uma linha de configuração.

### 🔧 Manual (`Redis__Mode: PubSub`)
Implementação explícita do Pub/Sub. Mostra exatamente o que acontece por baixo dos panos.

---

## Comandos Úteis

```bash
# Ver logs de todos os servidores
docker-compose logs -f

# Ver logs de um servidor específico
docker-compose logs -f server-1

# Ver mensagens passando pelo Redis
docker exec -it signalr-redis redis-cli monitor

# Parar tudo
docker-compose down
```

---

## Resumo

| Sem Redis | Com Redis |
|-----------|-----------|
| Cada servidor isolado | Servidores conectados |
| Mensagem fica presa | Mensagem propaga |
| Não escala | Escala horizontal ✅ |
