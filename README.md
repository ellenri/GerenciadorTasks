# GerenciadorTasks — Missão Recompensa (Backend)

API REST em **.NET 10** (Clean Architecture) para um gerenciador de tarefas
infantil com **gamificação**: crianças acumulam pontos ao concluir missões e
trocam por recompensas.

> Backend do app **Missão Recompensa**. O frontend (Astro/SSR) está em repositório
> separado e consome esta API.

---

## 🏗️ Arquitetura (Clean / DDD)

As dependências sempre apontam **para dentro** (Core não conhece ninguém):

```
GerenciadorTasksApi  ──▶  Application  ──▶  Core (domínio)
        │                  ▲
        └──▶  Infrastructure ─┘  (implementa as interfaces de Application)
```

| Camada | Responsabilidade |
|--------|------------------|
| **Core** | Entidades de domínio ricas (`Child`, `TaskItem`, `Reward`, `Notification`, `User`, `Justification`), enums, exceções. Sem dependências. Regras e invariantes vivem aqui. |
| **Application** | Casos de uso (`*Service`), DTOs, abstrações (`I*Repository`, `IUnitOfWork`, `IPasswordHasher`). Orquestra agregados em transações. |
| **Infrastructure** | EF Core (SQLite), repositórios concretos (`Ef*Repository`), `BCryptPasswordHasher`, `AppDbContext`, migrations, seed. |
| **GerenciadorTasksApi** | ASP.NET Core: controllers REST, middleware (auth cookie, CORS, exception handler), composição (`Program.cs`). |
| **UnitTests** | xUnit — entidades e serviços (com fakes). |

### Padrões chave
- **Domain Exception → HTTP 400**: `DomainException` é traduzida em `ProblemDetails` (RFC 7807) por um `IExceptionHandler` global. Controllers sem `try/catch` repetido.
- **Unit of Work**: várias mudanças confirmadas numa transação (ex.: concluir missão + creditar pontos é atômico).
- **Inversão de dependência**: serviços dependem de interfaces (`Application.Abstractions`); a implementação é registrada no `Program.cs`.

---

## 🚀 Como rodar

Pré-requisitos: **.NET 10 SDK**.

```bash
dotnet tool restore          # restaura o dotnet-ef (tool local)
dotnet run --project GerenciadorTasksApi
# API em http://localhost:5104
```

Na 1ª execução o app:
1. aplica as **migrations** (`Database.Migrate()`);
2. executa o **seed** — cria um responsável padrão + 3 crianças + um catálogo de recompensas.

### Credenciais de desenvolvimento (seed)
```
E-mail:  responsavel@exemplo.com
Senha:   123456
```

Banco: SQLite em `GerenciadorTasksApi/gerenciador.db` (ignorado pelo git).

---

## 📡 Endpoints

Todos exigem autenticação (cookie), exceto os de auth.

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/auth/register` | Cadastra responsável + emite cookie |
| POST | `/api/auth/login` | Autentica + emite cookie |
| POST | `/api/auth/logout` | Encerra sessão |
| GET  | `/api/auth/me` | Usuário atual |
| GET / POST | `/api/children` | Lista / cadastra criança |
| GET  | `/api/children/{id}` | Detalhe |
| GET / POST | `/api/tasks` | Lista / cria missão |
| GET  | `/api/tasks/{id}` | Detalhe |
| POST | `/api/tasks/{id}/complete` | Conclui (credita pontos) |
| GET / POST | `/api/rewards` | Lista / cria recompensa |
| POST | `/api/rewards/{id}/redeem` | Resgata (desconta pontos) |
| GET  | `/api/notifications` | Notificações do usuário |
| GET  | `/api/notifications/unread-count` | Total de não-lidas |
| POST | `/api/notifications/{id}/read` | Marca como lida |

OpenAPI disponível em `/openapi` em desenvolvimento.

---

## 🧪 Testes

```bash
dotnet test            # 46 testes (entidades + serviços)
```

---

## 🗃️ Migrations (EF Core)

```bash
# Criar
dotnet ef migrations add NomeDaMudanca \
  --project GerenciadorTasks.Infrastructure --startup-project GerenciadorTasksApi

# Aplicar manualmente (opcional; o startup aplica em runtime)
dotnet ef database update \
  --project GerenciadorTasks.Infrastructure --startup-project GerenciadorTasksApi
```

---

## 🔐 Segurança

- **Senhas**: hash com **BCrypt** (`BCryptPasswordHasher`, work factor 11). Nunca em texto plano.
- **Sessão**: cookie **HttpOnly** + `SameSite=Lax` (mitiga CSRF em POSTs cross-site).
- **Autorização**: todos os endpoints de domínio com `[Authorize]`.
- Dependências com **pin de versão** para corrigir vulnerabilidades conhecidas (`Microsoft.OpenApi`, `SQLitePCLRaw`).

> Para produção cross-domain: trocar `SameSite=Lax` por `None` + `Secure` (HTTPS).

---

## 🧭 Evolução do projeto (histórico)

| Fase | O que resolveu |
|------|----------------|
| **0** | Destravar o build (removeu uma "geração" de código duplicada/dead) |
| **1** | Adotou EF Core migrations (`EnsureCreated` → `Migrate`) |
| **2** | Autenticação por cookie HttpOnly + BCrypt |
| **3** | Recompensas (loop missão → pontos → resgate) |
| **4** | Notificações (geradas em eventos + contador) |

---

## 📁 Estrutura

```
GerenciadorTasks/
├── GerenciadorTasks.Core/          # domínio
├── GerenciadorTasks.Application/   # casos de uso + abstrações
├── GerenciadorTasks.Infrastructure/# EF Core, repositórios, segurança
├── GerenciadorTasksApi/            # ASP.NET Core (host)
├── GerenciadorTasks.UnitTests/     # xUnit
└── GerenciadorTasks.slnx
```
