# Coopertáxi Recibos

Sistema de emissão de recibos em PDF para a Coopertáxi Jundiaí. Motoristas emitem e compartilham recibos diretamente pelo celular em segundos, via WhatsApp ou download. Disponível como PWA e como aplicativo Android (via Capacitor).

---

## Sumário

- [Visão geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Requisitos](#requisitos)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Configuração](#configuração)
- [Variáveis de ambiente](#variáveis-de-ambiente)
- [Primeiro SystemAdmin](#primeiro-systemadmin)
- [Perfis de usuário e permissões](#perfis-de-usuário-e-permissões)
- [Fluxo de autenticação](#fluxo-de-autenticação)
- [Funcionalidades](#funcionalidades)
- [Endpoints da API](#endpoints-da-api)
- [PDF gerado](#pdf-gerado)
- [Banco de dados](#banco-de-dados)
- [Executar localmente](#executar-localmente)
- [Testes](#testes)
- [Deploy](#deploy)
- [Android / PWA](#android--pwa)
- [Roadmap](#roadmap)

---

## Visão geral

O sistema substitui o processo manual de emissão de recibos em papel. O motorista faz login, preenche os dados da corrida e compartilha o PDF gerado diretamente pelo WhatsApp — tudo pelo celular.

Administradores da cooperativa gerenciam usuários, acompanham recibos de qualquer motorista, geram relatórios mensais e podem cancelar ou redefinir senhas sem depender de suporte técnico.

---

## Arquitetura

Clean Architecture em quatro camadas:

```
ReceiptGenerator.Domain          ← Entidades, regras de negócio, contratos de repositório
ReceiptGenerator.Application     ← Casos de uso, DTOs, interfaces de infraestrutura
ReceiptGenerator.Infrastructure  ← EF Core, PostgreSQL, JWT, BCrypt, QuestPDF, geração de PDF
ReceiptGenerator.Api             ← Controllers HTTP, autenticação, composição de dependências
web/                             ← Frontend Angular 17 (standalone components)
```

Dependências fluem sempre de fora para dentro: `Api → Application → Domain`. A infraestrutura implementa as interfaces definidas no domínio — o domínio não conhece EF Core nem QuestPDF.

---

## Tecnologias

### Backend
| Componente | Tecnologia |
|---|---|
| Runtime | .NET 9 / ASP.NET Core 9 |
| ORM | Entity Framework Core 8 (Npgsql) |
| Banco de dados | PostgreSQL 16 |
| Autenticação | JWT Bearer (System.IdentityModel.Tokens.Jwt) |
| Hashing de senha | BCrypt.Net-Next |
| Geração de PDF | QuestPDF 2024.3 |
| Testes | xUnit, NSubstitute, AwesomeAssertions |

### Frontend
| Componente | Tecnologia |
|---|---|
| Framework | Angular 17 (standalone components) |
| Estilo | CSS puro (sem framework) |
| HTTP | HttpClient com interceptor de refresh automático |
| Compartilhamento de PDF | Web Share API (fallback: download automático) |
| Mobile app | Capacitor 6 (Android) |

---

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [PostgreSQL 16+](https://www.postgresql.org/) (ou Docker)
- [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

---

## Estrutura do projeto

```
ReceiptGenerator/
├── src/
│   ├── ReceiptGenerator.Api/           # Controllers, Program.cs, Dockerfile
│   ├── ReceiptGenerator.Application/   # Services, DTOs, interfaces
│   ├── ReceiptGenerator.Domain/        # Entidades, repositórios (interfaces), UserRole
│   └── ReceiptGenerator.Infrastructure/
│       ├── Pdf/                        # QuestReceiptPdfGenerator (QuestPDF + SVG)
│       ├── Persistence/                # ApplicationDbContext, Repositories, Migrations
│       └── Security/                   # JwtTokenGenerator, RefreshTokenGenerator, BcryptPasswordHasher
├── tests/
│   └── ReceiptGenerator.Tests/
│       ├── Application/                # AuthService, ClientService, ReceiptService, ReportService, UserService
│       ├── Domain/                     # Client, Receipt, User, UserRole
│       └── Validation/                 # Atributos de validação nos DTOs
├── web/                                # Frontend Angular
│   ├── src/app/
│   │   ├── clients/                    # CRUD de clientes
│   │   ├── login/                      # Tela de login
│   │   ├── navigation/                 # Header responsivo com menu do usuário
│   │   ├── profile/                    # Edição de perfil (nome, telefone, e-mail, senha)
│   │   ├── receipts/                   # Emissão, edição, cancelamento e compartilhamento de recibos
│   │   ├── report/                     # Relatório mensal com exportação CSV e impressão
│   │   ├── users/                      # Gestão de usuários (admin)
│   │   ├── auth.service.ts             # Login, logout, refresh, extração de claims do JWT
│   │   ├── user.service.ts             # Usuários e perfil
│   │   ├── receipt.service.ts          # Recibos, PDF, relatório mensal
│   │   ├── client.service.ts           # Clientes
│   │   ├── cep.service.ts              # Consulta ViaCEP para preenchimento automático de endereço
│   │   ├── share.service.ts            # Web Share API + fallback download
│   │   └── auth.interceptor.ts         # Refresh automático de token
│   └── android/                        # Projeto Android gerado pelo Capacitor
├── docker-compose.yml
├── render.yaml
└── MELHORIAS.md                        # Backlog de funcionalidades
```

---

## Configuração

### appsettings.json (valores padrão)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=receipt_generator;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "Secret": "change-this-development-secret-with-at-least-32-characters",
    "AccessTokenExpiryMinutes": 60
  },
  "BootstrapAdmin": {
    "Enabled": false
  },
  "Cooperative": {
    "Name": "COOPERTÁXI JUNDIAÍ",
    "LegalName": "Cooperativa de Trabalho dos Taxistas de Jundiaí - SP",
    "TaxId": "44.327.517/0001-65",
    "Phone": "(11) 97474-9974",
    "Email": "faleconosco@coopertaxijundiaisp.com.br",
    "City": "Jundiaí"
  },
  "Cors": {
    "AllowedOrigins": ""
  }
}
```

Os dados da cooperativa aparecem no cabeçalho de todos os PDFs gerados.

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=receipt_generator;Username=postgres;Password=SUA_SENHA_LOCAL"
  },
  "Cors": {
    "AllowedOrigins": "http://localhost:4200,https://localhost:4200"
  }
}
```

---

## Variáveis de ambiente

Em produção, todas as configurações sensíveis são passadas como variáveis de ambiente (formato `Secao__Chave`):

| Variável | Descrição |
|---|---|
| `ConnectionStrings__DefaultConnection` | Connection string do PostgreSQL |
| `JwtSettings__Secret` | Segredo HMAC-SHA256 para assinar JWTs (mínimo 32 caracteres) |
| `Cors__AllowedOrigins` | URLs do frontend separadas por vírgula (sem barra no final) |
| `Cooperative__Name` | Nome da cooperativa exibido no PDF |
| `Cooperative__LegalName` | Razão social |
| `Cooperative__TaxId` | CNPJ |
| `Cooperative__Phone` | Telefone |
| `Cooperative__Email` | E-mail |
| `Cooperative__City` | Cidade (usada no campo "Local e data" do PDF) |
| `BootstrapAdmin__Enabled` | `false` em produção após criar o primeiro admin |

---

## Primeiro SystemAdmin

Em uma base limpa, habilite temporariamente o bootstrap via User Secrets:

```bash
dotnet user-secrets set "BootstrapAdmin:Enabled" "true" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
dotnet user-secrets set "BootstrapAdmin:Username" "admin" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
dotnet user-secrets set "BootstrapAdmin:Password" "SENHA_FORTE_AQUI" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

Ao iniciar a API, se não existir nenhum `SystemAdmin`, o usuário inicial é criado. Após o primeiro login, desative:

```bash
dotnet user-secrets set "BootstrapAdmin:Enabled" "false" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

> **Mantenha este recurso desligado em produção** após criar o primeiro administrador.

---

## Perfis de usuário e permissões

| Perfil | Valor no banco | Acesso |
|---|---|---|
| **Administrador do sistema** | `SystemAdmin` | Acesso total: gerencia usuários (todos os perfis), clientes, recibos e PDFs de qualquer motorista |
| **Administrador da cooperativa** | `CoopAdmin` | Mesmas permissões do SystemAdmin, exceto criar outros SystemAdmin |
| **Motorista** | `Driver` | Gerencia apenas seus próprios clientes e recibos; acessa relatório dos próprios dados |

### Restrições de negócio por perfil

- **Criação de usuários**: `CoopAdmin` só pode criar contas com perfil `Driver`
- **Desativação**: nenhum usuário pode desativar a própria conta
- **Recibos cancelados**: apenas admins podem ver recibos cancelados (parâmetro `includeCancelled=true`)
- **Relatório**: drivers sempre veem apenas seus próprios dados; admins podem filtrar por motorista
- **Reset de senha**: somente admins podem redefinir a senha de outro usuário

---

## Fluxo de autenticação

```
1. POST /api/auth/login  →  { accessToken, refreshToken, expiresIn: 3600 }
   - accessToken: JWT válido por 60 minutos
   - refreshToken: token opaco válido por 180 dias, armazenado como hash no banco

2. Todas as requisições autenticadas enviam:
   Authorization: Bearer {accessToken}

3. Quando o accessToken expira, o interceptor Angular chama automaticamente:
   POST /api/auth/refresh  →  novo par de tokens (rotação de refresh token)

4. POST /api/auth/logout
   - Revoga o refresh token no servidor (invalida o hash no banco)
   - Limpa os tokens do localStorage no cliente
```

**Claims do JWT:**
- `NameIdentifier` → ID do usuário
- `Name` → username (login)
- `Role` → perfil (`SystemAdmin`, `CoopAdmin` ou `Driver`)
- `fullName` → nome completo (exibido no cabeçalho da aplicação)

**Troca de senha:** ao alterar a senha via `PUT /api/users/me`, o refresh token é invalidado imediatamente, forçando novo login na próxima expiração do accessToken.

**Rate limiting:** os endpoints `/api/auth/login` e `/api/auth/refresh` possuem limitação de taxa por IP — 5 req/min (janela deslizante) e 30 req/min (janela fixa), respectivamente. Exceder o limite retorna HTTP 429 com cabeçalho `Retry-After`.

---

## Funcionalidades

### Emissão de recibos

O motorista acessa **Recibos → Criar Novo Recibo** e preenche:

| Campo | Obrigatório | Detalhe |
|---|---|---|
| Cliente | Sim | Digitado via autocomplete; se não existir, é criado automaticamente |
| Valor | Sim | Formatado como moeda brasileira em tempo real |
| Descrição | Sim | Default: "Serviço de Táxi" |
| Data do serviço | Não | Data formatada como dd/MM/AAAA no PDF |
| Horário início / fim | Não | Visíveis ao expandir "Detalhes extras" |
| Telefone do emissor | Não | Pré-preenchido do perfil do motorista logado |
| E-mail do emissor | Não | Pré-preenchido do perfil do motorista logado |

**Numeração**: cada motorista tem uma sequência própria, independente dos outros.

**DriverName imutável**: o nome do motorista é gravado como snapshot em `Receipt.DriverName` no momento da criação. Alterações posteriores no cadastro não afetam recibos já emitidos.

**Admin emitindo em nome de outro motorista**: ao criar um recibo, o admin pode selecionar o motorista responsável via campo `driverUserId`. O recibo é criado com a numeração e o nome daquele motorista.

### Filtro de recibos por período

A listagem de recibos suporta filtros opcionais de mês e ano via seletores na interface. A paginação é reiniciada ao aplicar ou limpar filtros. São exibidos 20 recibos por página.

### Cancelamento de recibos (soft delete)

O botão "Excluir" abre um prompt solicitando o motivo do cancelamento (opcional). Ao confirmar:

- O recibo não é removido do banco — recebe `CancelledAt` (timestamp) e `CancelReason` (até 500 caracteres)
- Recibos cancelados desaparecem da listagem padrão
- O PDF de um recibo cancelado exibe a marca **"CANCELADO"** em vermelho diagonal sobre o documento
- Admins podem visualizar recibos cancelados enviando `includeCancelled=true` na query

### Geração e compartilhamento de PDF

Ao clicar em **Compartilhar**:

1. O frontend solicita o PDF ao backend: `GET /api/receipts/{id}/pdf`
2. O PDF é gerado em memória pelo QuestPDF e retornado como `application/pdf`
3. Se o dispositivo suportar **Web Share API** (Android/iOS), o PDF é compartilhado diretamente (WhatsApp, e-mail, Drive, etc.)
4. Caso contrário, o arquivo é baixado automaticamente como `recibo-XXXXXX.pdf`

### Gestão de clientes

CRUD completo de clientes (passageiros):
- Campos: **Nome** (obrigatório), **CEP** (com preenchimento automático), **Logradouro**, **Número**, **Complemento**, **Bairro**, **Cidade**, **UF**, **CPF/CNPJ** (opcional)
- **CEP auto-fill**: ao digitar o CEP completo, os campos de logradouro, complemento, bairro, cidade e UF são preenchidos automaticamente via ViaCEP
- Listagem com busca por nome via `<datalist>`
- Formulário com indicador visual de edição (borda amarela), dirty tracking e confirmação ao cancelar com alterações pendentes
- Exclusão via painel de confirmação (bottom sheet)
- Toast de feedback após salvar ou excluir
- Clientes são vinculados ao motorista — cada motorista vê apenas os seus

### Perfil do usuário

Qualquer usuário logado pode acessar **Meu perfil** (menu do cabeçalho) e editar:

| Campo | Regra |
|---|---|
| Nome completo | Atualiza o nome exibido nos próximos recibos (não afeta recibos anteriores) |
| Telefone | Pré-preenche automaticamente o campo "Telefone do emissor" nos novos recibos |
| E-mail | Pré-preenche automaticamente o campo "E-mail do emissor" nos novos recibos |
| Senha | Exige confirmação da senha atual; ao alterar, o refresh token é invalidado |

### Gestão de usuários (admin)

Na tela **Usuários**, admins podem:
- Listar todos os usuários com nome, login, perfil e status
- Criar novos usuários (username, senha, perfil, nome completo)
- Ativar / desativar usuários
- **Redefinir a senha** de qualquer usuário sem precisar da senha atual (via painel bottom sheet)

### Relatório mensal

Disponível para **todos os perfis** (motoristas veem apenas os próprios dados):

- Tabela com: mês/ano, quantidade de recibos, valor médio, valor total
- Totalizador geral ao final
- **Filtros**: mês, ano, e (para admins) seleção de motorista específico
- **Exportar CSV**: download do relatório filtrado com BOM UTF-8 para compatibilidade com Excel
- **Imprimir**: aciona `window.print()` para impressão otimizada

---

## Endpoints da API

### Autenticação — `POST /api/auth/...` (público)

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/auth/login` | Login; retorna `accessToken`, `refreshToken` e `expiresIn` (3600s). Rate limit: 5 req/min por IP |
| POST | `/api/auth/refresh` | Renova o par de tokens usando o refresh token. Rate limit: 30 req/min por IP |
| POST | `/api/auth/logout` | Revoga o refresh token no servidor; requer autenticação |

### Perfil — `GET|PUT /api/users/me` (qualquer perfil autenticado)

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/users/me` | Retorna dados do usuário logado (inclui `phone`, `email`, `updatedAt`) |
| PUT | `/api/users/me` | Atualiza `fullName`, `phone`, `email` e/ou senha |

### Usuários — `/api/users` (requer `SystemAdmin` ou `CoopAdmin`)

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/users` | Lista todos os usuários |
| GET | `/api/users/drivers` | Lista motoristas ativos (para seletor de motorista nos recibos) |
| POST | `/api/users` | Cria usuário (`CoopAdmin` só pode criar `Driver`) |
| PUT | `/api/users/{id}/activate` | Ativa usuário |
| PUT | `/api/users/{id}/deactivate` | Desativa usuário (não pode desativar a si mesmo) |
| PUT | `/api/users/{id}/reset-password` | Redefine a senha; invalida o refresh token do usuário alvo |

### Clientes — `/api/clients` (qualquer perfil autenticado)

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/clients` | Lista clientes do motorista autenticado |
| GET | `/api/clients/{id}` | Detalhe do cliente (verifica propriedade) |
| POST | `/api/clients` | Cria cliente |
| PUT | `/api/clients/{id}` | Atualiza cliente |
| DELETE | `/api/clients/{id}` | Remove cliente |

### Recibos — `/api/receipts` (qualquer perfil autenticado)

| Método | Rota | Parâmetros | Descrição |
|---|---|---|---|
| GET | `/api/receipts` | `page`, `pageSize`, `month?`, `year?`, `includeCancelled?` | Lista paginada; motoristas veem apenas os seus; `includeCancelled` só funciona para admins |
| GET | `/api/receipts/{id}` | — | Detalhe do recibo |
| POST | `/api/receipts` | — | Cria recibo; admin pode informar `driverUserId` para emitir em nome de outro |
| PUT | `/api/receipts/{id}` | — | Atualiza recibo |
| DELETE | `/api/receipts/{id}` | body: `{ reason? }` | **Soft cancel**: define `CancelledAt` e `CancelReason`; não remove do banco |
| GET | `/api/receipts/{id}/pdf` | — | Gera e retorna o PDF; PDFs de recibos cancelados exibem marca d'água "CANCELADO" |

### Relatórios — `/api/reports` (qualquer perfil autenticado)

| Método | Rota | Parâmetros | Descrição |
|---|---|---|---|
| GET | `/api/reports/monthly-summary` | `year?`, `month?`, `driverId?` | Resumo mensal; `driverId` é ignorado para drivers (veem apenas os próprios dados) |

### Saúde — (público)

| Método | Rota | Descrição |
|---|---|---|
| GET | `/health` | Status do processo da API |
| GET | `/health/ready` | Status da conexão com o banco de dados |

---

## PDF gerado

Cada recibo em PDF contém:

- **Cabeçalho**: logotipo da cooperativa, nome, CNPJ, telefone, e-mail e número do recibo (`XXXXXX/AAAA`)
- **Linha de destaque** amarela separadora
- **Banda de valor**: valor em moeda e por extenso em português
- **Declaração**: "Recebemos de [nome do cliente], inscrito no CPF/CNPJ [número], o valor de..."
- **Detalhes do serviço**: descrição, data(s) do serviço, horário início/fim, endereço do cliente
- **Local e data** por extenso
- **Assinatura visual**: nome do motorista na fonte cursiva Dancing Script, seguido de linha, nome impresso e cargo "Motorista". Se informados, exibe telefone e e-mail do emissor abaixo da assinatura
- **Rodapé**: timestamp de emissão eletrônica
- **Marca d'água CANCELADO** (recibos cancelados): texto em vermelho semi-transparente diagonal, gerado via SVG sobreposto ao documento inteiro

Fontes embutidas no assembly (sem dependência do sistema operacional):
- **Lato** — texto geral
- **Dancing Script** — assinatura visual

---

## Banco de dados

### Tabelas

| Tabela | Colunas relevantes |
|---|---|
| `Users` | `Id`, `Username` (único), `PasswordHash`, `FullName`, `Role`, `IsActive`, `Phone`, `Email`, `UpdatedAt`, `RefreshTokenHash`, `RefreshTokenExpiry` |
| `Clients` | `Id`, `Name`, `Address`, `TaxId`, `ZipCode`, `Street`, `Number`, `Complement`, `Neighborhood`, `City`, `State`, `UserId` (FK → Users CASCADE) |
| `Receipts` | `Id`, `Number`, `Date`, `Description`, `Amount`, `StartTime`, `EndTime`, `ServiceDates`, `IssuerName`, `IssuerPhone`, `IssuerEmail`, `DriverName`, `CancelledAt`, `CancelReason`, `ClientId` (FK RESTRICT), `UserId` (FK RESTRICT) |

### Índices notáveis

- `Users.Username`: único
- `Receipts(UserId, Date DESC)`: índice composto para queries paginadas por motorista/período

### Migrations (ordem cronológica)

| Migration | O que cria/altera |
|---|---|
| `InitialCreate` | Tabelas Users, Clients, Receipts com FKs |
| `AddUserRoles` | `IsActive` (default true) e `Role` em Users |
| `AddReceiptNumber` | `Number` (integer) em Receipts |
| `RenameRoles` | Renomeia `SuperAdmin → SystemAdmin`, `Operator → Driver` nos dados |
| `AddRefreshTokenColumns` | `RefreshTokenHash` e `RefreshTokenExpiry` em Users |
| `AddReceiptDateIndex` | Substitui índice simples por índice composto `(UserId, Date DESC)` |
| `AddUserFullName` | `FullName` (varchar 200) em Users |
| `AddUserContactAndReceiptCancellation` | `Phone`, `Email`, `UpdatedAt` em Users; `CancelledAt`, `CancelReason` em Receipts |
| `AddClientAddressFields` | `ZipCode`, `Street`, `Number`, `Complement`, `Neighborhood`, `City`, `State` (nullable) em Clients |

Para aplicar todas as migrations pendentes:

```bash
dotnet ef database update \
  --project src/ReceiptGenerator.Infrastructure/ReceiptGenerator.Infrastructure.csproj \
  --startup-project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

Para criar uma nova migration:

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/ReceiptGenerator.Infrastructure/ReceiptGenerator.Infrastructure.csproj \
  --startup-project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

---

## Executar localmente

### 1. Backend

```bash
dotnet restore ReceiptGenerator.sln
dotnet build ReceiptGenerator.sln

# Aplicar migrations
dotnet ef database update \
  --project src/ReceiptGenerator.Infrastructure/ReceiptGenerator.Infrastructure.csproj \
  --startup-project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj

# Iniciar a API (porta padrão: 5281)
dotnet run --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

A especificação OpenAPI fica disponível em `/openapi/v1.json` e o Swagger UI em `/swagger` (ambiente `Development`).

### 2. Frontend

```bash
cd web
npm install
npm start
```

O proxy de desenvolvimento (`proxy.conf.json`) encaminha `/api/*` para `http://localhost:5281`.

### 3. Com Docker Compose

```bash
docker compose up --build
```

Sobe PostgreSQL 16, a API .NET e o frontend Angular em três contêineres. Acesse em `http://localhost:4200`.

---

## Testes

```bash
dotnet test tests/ReceiptGenerator.Tests/ReceiptGenerator.Tests.csproj
```

Suite atual: **193 testes** cobrindo camadas de domínio, aplicação e validação de DTOs.

| Arquivo | Camada testada |
|---|---|
| `Application/AuthServiceTests.cs` | Login, refresh, logout — validação de credenciais e tokens |
| `Application/ClientServiceTests.cs` | CRUD de clientes — ownership, validações e campos de endereço |
| `Application/ReceiptServiceTests.cs` | Criação, atualização, cancelamento, PDF e lógica de admin/driver |
| `Application/ReportServiceTests.cs` | Agrupamento mensal e escopo por motorista |
| `Application/UserServiceTests.cs` | CRUD de usuários, activate/deactivate, perfil, reset de senha |
| `Domain/ClientTests.cs` | Entidade Client — validações, campos de endereço, Update |
| `Domain/ReceiptTests.cs` | Entidade Receipt — Amount, datas, SetNumber, Cancel |
| `Domain/UserTests.cs` | Entidade User — construção, roles, refresh token, perfil |
| `Domain/UserRoleTests.cs` | UserRole estático — IsValid, IsAdmin, case-sensitivity |
| `Validation/DtoValidationTests.cs` | Atributos [Required], [MaxLength], [Range] nos DTOs via reflection |

Para coleta de cobertura (threshold mínimo de 75%):

```bash
dotnet test tests/ReceiptGenerator.Tests/ReceiptGenerator.Tests.csproj \
  --settings tests/ReceiptGenerator.Tests/coverlet.runsettings \
  --collect:"XPlat Code Coverage"
```

---

## Deploy

### Backend no Render

O arquivo `render.yaml` define a API como Web Service Docker.

1. Conecte o repositório GitHub.
2. Crie um Blueprint usando `render.yaml`.
3. Informe as variáveis de ambiente:
   - `ConnectionStrings__DefaultConnection`: connection string do Supabase (Session Pooler) com SSL
   - `Cors__AllowedOrigins`: URL pública do frontend, sem barra no final (ex: `https://recibos.pages.dev`)
4. Valide após o deploy:
   - `https://sua-api.onrender.com/health` — processo da API
   - `https://sua-api.onrender.com/health/ready` — conexão com o banco

O segredo JWT é gerado automaticamente pelo Render. O bootstrap de admin permanece desabilitado (`BootstrapAdmin__Enabled=false`).

> O plano gratuito pode suspender a API após inatividade. Para uso diário, use `plan: starter` no `render.yaml`.

Para apontar para o Supabase em desenvolvimento local:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.SEU_ID;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true" \
  --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

Para voltar ao banco local:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=receipt_generator;Username=postgres;Password=SUA_SENHA_LOCAL" \
  --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

### Frontend no Cloudflare Workers

1. Crie um Worker conectado ao repositório, diretório raiz: `web`
2. Comando de build: `npm run build`
3. Comando de deploy: `npm run deploy`
4. Variáveis de build:
   ```
   API_BASE_URL=https://sua-api.onrender.com/api
   NODE_VERSION=22
   ```
5. Após o primeiro deploy, atualize `Cors__AllowedOrigins` no Render com a URL `workers.dev` fornecida pela Cloudflare

O arquivo `web/wrangler.jsonc` configura o Static Assets do Angular e o fallback de SPA.

---

## Android / PWA

O frontend pode ser empacotado como aplicativo Android nativo via **Capacitor 6**.

```
App ID:    br.com.coopertaxijundiaisp.receipts
App Name:  Coopertáxi Recibos
WebDir:    dist/receipt-generator.frontend/browser
```

Para gerar o APK:

```bash
cd web
npm run build
npx cap sync android
npx cap open android   # Abre no Android Studio para gerar o APK/AAB
```

Como PWA, a aplicação pode ser instalada diretamente pelo navegador em Android e iOS via `manifest.webmanifest`.

> **Pendente**: os ícones do PWA/APK precisam ser gerados a partir do logotipo da cooperativa (ver Roadmap).

---

## Roadmap

Melhorias planejadas em `MELHORIAS.md`, organizadas por prioridade:

### Baixa prioridade (pendentes)
- **Ícones do PWA/APK** — gerar assets de imagem nos tamanhos exigidos pelo manifest (72px a 512px)
- **Recuperação de senha por e-mail** — fluxo "esqueci minha senha" com SMTP; alta complexidade
- **Paginação de clientes** — server-side com busca por nome; implementar somente se a lista crescer
- **Histórico de alterações de perfil** — tabela de auditoria `UserAuditLogs`; implementar sob demanda

### Já implementados (referência)
- Edição de perfil pelo motorista (`PUT /api/users/me`) ✅
- Filtro de recibos por período (mês/ano) ✅
- Dados do emissor pré-preenchidos do perfil ✅
- Relatório disponível para motoristas (escopo próprio) ✅
- Cancelamento lógico de recibos com marca d'água no PDF ✅
- Reset de senha pelo administrador ✅
- Campo `UpdatedAt` na entidade `User` ✅
- Exportação do relatório em CSV ✅
- Edição de clientes na interface ✅
- Campos estruturados de endereço no cadastro de clientes (logradouro, número, complemento, bairro, cidade, UF, CEP) ✅
- Preenchimento automático de endereço por CEP via ViaCEP ✅
- Rate limiting em login e refresh (proteção contra força bruta) ✅
- Redefinição de senha via painel bottom sheet (sem window.prompt) ✅
- Suíte de testes expandida para 193 testes unitários ✅
