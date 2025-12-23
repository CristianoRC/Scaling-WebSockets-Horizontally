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

```
        Usuário A                              Usuário B
            │                                      │
            ▼                                      ▼
       ┌─────────┐                           ┌─────────┐
       │Server-1 │──── PUBLICA ────►  Redis  │Server-2 │
       └─────────┘                     │     └─────────┘
                                       │
                         ◄── RECEBE ───┘
```

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

Acesse: **http://localhost:5000**

---

## O Que Está Rodando

| Container | Função |
|-----------|--------|
| **nginx** | Serve o frontend + distribui conexões |
| **server-1** | Instância 1 da API |
| **server-2** | Instância 2 da API |
| **server-3** | Instância 3 da API |
| **redis** | Ponte de comunicação |

---

## Teste Você Mesmo

1. Abra **3 abas** do navegador em http://localhost:5000
2. Veja que cada aba pode conectar em um **servidor diferente**
3. Envie uma mensagem em qualquer aba
4. **Todas as abas recebem!** 🎉

---

## Dois Modos de Implementação

### 🪄 Automático (`/chatHub`)
O SignalR faz tudo sozinho. Você só adiciona uma linha de configuração.

### 🔧 Manual (`/manualChatHub`)
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

## Arquitetura Visual

```
                         ┌──────────────┐
                         │    NGINX     │
                         │  porta 5000  │
                         └──────┬───────┘
                                │
          ┌─────────────────────┼─────────────────────┐
          │                     │                     │
          ▼                     ▼                     ▼
    ┌──────────┐          ┌──────────┐          ┌──────────┐
    │ Server-1 │          │ Server-2 │          │ Server-3 │
    │  (API)   │          │  (API)   │          │  (API)   │
    └────┬─────┘          └────┬─────┘          └────┬─────┘
         │                     │                     │
         └─────────────────────┼─────────────────────┘
                               │
                               ▼
                        ┌─────────────┐
                        │    REDIS    │
                        │ (mensageiro)│
                        └─────────────┘
```

---

## Resumo

| Sem Redis | Com Redis |
|-----------|-----------|
| Cada servidor isolado | Servidores conectados |
| Mensagem fica presa | Mensagem propaga |
| Não escala | Escala horizontal ✅ |
