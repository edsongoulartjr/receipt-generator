# ReceiptGenerator

Sistema de emissao de recibos em PDF para a Coopertaxi Jundiaisp, desenvolvido em .NET 9 com frontend Angular 17. Motoristas emitem e compartilham recibos pelo celular em segundos via WhatsApp.

## Estrutura

- `src/ReceiptGenerator.Domain`: entidades, regras de negocio e contratos de repositorio.
- `src/ReceiptGenerator.Application`: casos de uso, DTOs e abstracoes de infraestrutura.
- `src/ReceiptGenerator.Infrastructure`: EF Core, PostgreSQL, repositorios, JWT, BCrypt, QuestPDF e geracao de PDF.
- `src/ReceiptGenerator.Api`: controllers HTTP, autenticacao JWT Bearer, CORS e composicao de dependencias.
- `web`: frontend Angular 17 (standalone components) para emissao, consulta e compartilhamento de recibos.

## Perfis de Acesso

| Perfil | Valor no banco | Descricao |
|---|---|---|
| Administrador do sistema | `SystemAdmin` | Gerencia usuarios, clientes, recibos e PDFs de qualquer motorista |
| Administrador da cooperativa | `CoopAdmin` | Mesmas permissoes de administrador, exceto criar outros administradores do sistema |
| Motorista | `Driver` | Gerencia apenas seus proprios clientes e recibos |

O cadastro publico de usuarios foi removido. Novos usuarios sao criados por um administrador na tela de Usuarios.

## Fluxo principal

**Motorista:**
1. Faz login com usuario e senha.
2. Cadastra clientes (passageiros) uma unica vez.
3. Emite um recibo vinculado a um cliente — o nome do motorista e preenchido automaticamente a partir do perfil autenticado.
4. Compartilha o PDF gerado diretamente pelo WhatsApp via Web Share API.

**Administrador:**
1. Faz login.
2. Seleciona o motorista responsavel ao emitir um recibo em nome de outro.
3. Acessa recibos e PDFs de qualquer motorista.
4. Gerencia usuarios (criacao, ativacao, desativacao).

## Regras de negocio importantes

- **DriverName imutavel**: o nome do motorista e gravado como snapshot em `Receipt.DriverName` no momento da criacao, a partir de `User.FullName`. Alteracoes posteriores no cadastro do motorista nao afetam recibos ja emitidos.
- **Numeracao por motorista**: cada motorista tem uma sequencia propria de numeracao de recibos (`Receipt.Number`), independente dos outros.
- **Refresh token**: o JWT de acesso expira em poucos minutos; o frontend renova automaticamente usando o refresh token sem interrupcao da sessao.

## PDF gerado

O recibo em PDF inclui:

- Cabecalho com logotipo, dados da cooperativa (nome, CNPJ, telefone, e-mail) e numero do recibo.
- Banda de valor destacada com o valor por extenso.
- Declaracao de recebimento com nome e CPF/CNPJ do cliente.
- Detalhes do servico: descricao, data(s), horario de inicio/fim e endereco do cliente.
- Data de emissao por extenso.
- **Assinatura visual**: nome do motorista renderizado na fonte cursiva Dancing Script, acima de uma linha de assinatura, seguido do nome impresso e do rotulo "Motorista".
- Rodape com timestamp de emissao eletronica.

As fontes (Lato + Dancing Script) sao embutidas no assembly — sem dependencia de fontes do sistema operacional.

## Primeiro SystemAdmin

Em uma base limpa, habilite temporariamente o bootstrap do administrador inicial via User Secrets:

```bash
dotnet user-secrets set "BootstrapAdmin:Enabled" "true" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
dotnet user-secrets set "BootstrapAdmin:Username" "admin" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
dotnet user-secrets set "BootstrapAdmin:Password" "SENHA_FORTE_AQUI" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

Ao iniciar a API, se ainda nao existir nenhum `SystemAdmin`, o usuario inicial sera criado. Depois do primeiro login, desative o bootstrap:

```bash
dotnet user-secrets set "BootstrapAdmin:Enabled" "false" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

Mantenha esse recurso desligado em producao apos criar o primeiro administrador.

## Endpoints

### Autenticacao
- `POST /api/auth/login` — login com username/password; retorna access token e refresh token
- `POST /api/auth/refresh` — renova o access token usando o refresh token
- `POST /api/auth/logout` — invalida o refresh token no servidor

### Usuarios _(requer perfil admin)_
- `GET /api/users` — lista todos os usuarios
- `GET /api/users/drivers` — lista motoristas ativos (usado pelo frontend para o seletor de motorista)
- `POST /api/users` — cria usuario; `CoopAdmin` so pode criar `Driver`
- `PUT /api/users/{id}/activate` — ativa usuario
- `PUT /api/users/{id}/deactivate` — desativa usuario (nao pode desativar a si mesmo)

### Clientes
- `GET /api/clients` — lista clientes do motorista autenticado
- `POST /api/clients` — cria cliente
- `GET /api/clients/{id}` — detalhe do cliente
- `PUT /api/clients/{id}` — atualiza cliente
- `DELETE /api/clients/{id}` — remove cliente

### Recibos
- `GET /api/receipts` — lista recibos paginados; admin ve todos, motorista ve apenas os seus
- `POST /api/receipts` — emite recibo; motorista emite para si; admin pode informar `driverUserId` para emitir em nome de outro
- `GET /api/receipts/{id}` — detalhe do recibo
- `PUT /api/receipts/{id}` — atualiza recibo
- `DELETE /api/receipts/{id}` — exclui recibo
- `GET /api/receipts/{id}/pdf` — gera e retorna o PDF do recibo

### Relatorios
- `GET /api/reports/monthly-summary` — resumo mensal de recibos (quantidade, total e media por mes)

### Saude
- `GET /health` — status do processo da API
- `GET /health/ready` — status da conexao com o banco de dados

## Configuracao

Por padrao, o ambiente `Development` aponta para PostgreSQL local.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=receipt_generator;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "Secret": "change-this-development-secret-with-at-least-32-characters",
    "AccessTokenExpiryMinutes": 15
  },
  "Cooperative": {
    "Name": "COOPERTÁXI JUNDIAÍ",
    "LegalName": "Cooperativa de Trabalho dos Taxistas de Jundiaí - SP",
    "TaxId": "44.327.517/0001-65",
    "Phone": "(11) 97474-9974",
    "Email": "faleconosco@coopertaxijundiaisp.com.br",
    "City": "Jundiaí"
  }
}
```

Os dados da cooperativa aparecem no cabecalho do PDF gerado. Em producao, podem ser sobrescritos via variaveis de ambiente no Render: `Cooperative__Name`, `Cooperative__TaxId`, `Cooperative__Phone`, etc.

Para apontar para Supabase sem gravar senha no Git, use User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=aws-1-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.fodubkyzdyozxflnaijv;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

Para voltar ao banco local:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=receipt_generator;Username=postgres;Password=SUA_SENHA_LOCAL" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

## Executar

```bash
dotnet restore ReceiptGenerator.sln
dotnet build ReceiptGenerator.sln
dotnet run --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

## Testes

```bash
dotnet test tests/ReceiptGenerator.Tests/ReceiptGenerator.Tests.csproj
```

Para executar com coleta de cobertura (threshold minimo de 75%):

```bash
dotnet test tests/ReceiptGenerator.Tests/ReceiptGenerator.Tests.csproj \
  --settings tests/ReceiptGenerator.Tests/coverlet.runsettings \
  --collect:"XPlat Code Coverage"
```

Durante desenvolvimento, a especificacao OpenAPI fica disponivel em `/openapi/v1.json`.
O Swagger UI fica disponivel em `/swagger`.

Para executar o frontend:

```bash
cd web
npm install
npm start
```

O frontend usa `/api` e o `proxy.conf.json` encaminha as chamadas para `http://localhost:5281` em desenvolvimento.

## Migration

Para aplicar todas as migrations pendentes no banco configurado:

```bash
dotnet ef database update --project src/ReceiptGenerator.Infrastructure/ReceiptGenerator.Infrastructure.csproj --startup-project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

Para criar uma nova migration apos alterar entidades:

```bash
dotnet ef migrations add NomeDaMigration --project src/ReceiptGenerator.Infrastructure/ReceiptGenerator.Infrastructure.csproj --startup-project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

## Deploy com Docker

```bash
docker compose up --build
```

Antes de publicar em producao, troque `JwtSettings__Secret` e as credenciais do PostgreSQL em `docker-compose.yml`.

## Publicacao

### Backend no Render

O arquivo `render.yaml` define a API como um Web Service Docker. No Render:

1. Conecte o repositorio GitHub `edsongoulartjr/receipt-generator`.
2. Crie um Blueprint usando o arquivo `render.yaml` da raiz.
3. Informe as variaveis solicitadas:
   - `ConnectionStrings__DefaultConnection`: connection string do Session Pooler do Supabase, com SSL.
   - `Cors__AllowedOrigins`: URL publica exata do frontend, sem barra no final. Exemplo: `https://receipt-generator.pages.dev`.
4. Aguarde o deploy e valide:
   - `https://SUA-API.onrender.com/health`: processo da API.
   - `https://SUA-API.onrender.com/health/ready`: conexao com o banco.

O segredo JWT e gerado pelo Render. O bootstrap do administrador permanece desabilitado em producao.

O Blueprint usa o plano gratuito para homologacao. Nesse plano, a API pode ser suspensa apos alguns minutos sem trafego, fazendo o primeiro acesso seguinte demorar mais. Para uso diario da cooperativa, altere `plan: free` para `plan: starter`.

### Frontend no Cloudflare Workers

Depois que a API estiver publicada:

1. Crie um Worker conectado ao mesmo repositorio.
2. Defina o diretorio raiz como `web`.
3. Use o comando de build `npm run build`.
4. Use o comando de deploy `npm run deploy`.
5. Adicione as variaveis de build:

```text
API_BASE_URL=https://SUA-API.onrender.com/api
NODE_VERSION=22
```

O arquivo `web/wrangler.jsonc` publica o build Angular como Static Assets e configura fallback de SPA. Depois do primeiro deploy, atualize `Cors__AllowedOrigins` no Render com a URL HTTPS `workers.dev` fornecida pela Cloudflare. Mais de uma origem pode ser informada separando os valores por virgula.
