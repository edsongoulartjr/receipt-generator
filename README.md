# ReceiptGenerator

Projeto .NET 9 para emissao de recibos em PDF, organizado em camadas com orientacao a DDD.

## Estrutura

- `src/ReceiptGenerator.Domain`: entidades, regras basicas de negocio e contratos de repositorio.
- `src/ReceiptGenerator.Application`: casos de uso, DTOs e abstracoes para infraestrutura.
- `src/ReceiptGenerator.Infrastructure`: EF Core, PostgreSQL, repositories, JWT, BCrypt e QuestPDF.
- `src/ReceiptGenerator.Api`: controllers HTTP, autenticacao, CORS e composicao de dependencias.
- `web`: frontend Angular para emissao e consulta de recibos.

## Fluxo principal

1. Autenticar um usuario.
2. Cadastrar clientes do usuario autenticado.
3. Emitir recibos vinculados a um cliente.
4. Baixar o recibo em PDF pelo endpoint `GET /api/receipts/{id}/pdf`.

## Perfis de Acesso

- `SuperAdmin`: gerencia usuarios, clientes, recibos e PDFs.
- `Operator`: gerencia clientes, recibos e PDFs.

O cadastro publico de usuarios foi removido. Novos usuarios devem ser criados por um `SuperAdmin`.

## Primeiro SuperAdmin

Em uma base limpa, como uma nova base no Supabase, habilite temporariamente o bootstrap do administrador inicial via User Secrets:

```bash
dotnet user-secrets set "BootstrapAdmin:Enabled" "true" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
dotnet user-secrets set "BootstrapAdmin:Username" "admin" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
dotnet user-secrets set "BootstrapAdmin:Password" "SENHA_FORTE_AQUI" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

Ao iniciar a API, se ainda nao existir nenhum `SuperAdmin`, o usuario inicial sera criado. Depois do primeiro login, desative o bootstrap:

```bash
dotnet user-secrets set "BootstrapAdmin:Enabled" "false" --project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
```

Mantenha esse recurso desligado em producao apos criar o primeiro administrador.

## Endpoints

- `POST /api/auth/login`
- `GET /api/users`
- `POST /api/users`
- `PUT /api/users/{id}/activate`
- `PUT /api/users/{id}/deactivate`
- `GET /api/clients`
- `POST /api/clients`
- `GET /api/clients/{id}`
- `PUT /api/clients/{id}`
- `DELETE /api/clients/{id}`
- `GET /api/receipts`
- `POST /api/receipts`
- `GET /api/receipts/{id}`
- `PUT /api/receipts/{id}`
- `DELETE /api/receipts/{id}`
- `GET /api/receipts/{id}/pdf`

## Configuracao

Por padrao, o ambiente `Development` aponta para PostgreSQL local.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=receipt_generator;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "Secret": "change-this-development-secret-with-at-least-32-characters"
  }
}
```

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

```bash
dotnet ef database update --project src/ReceiptGenerator.Infrastructure/ReceiptGenerator.Infrastructure.csproj --startup-project src/ReceiptGenerator.Api/ReceiptGenerator.Api.csproj
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
4. Aguarde o deploy e valide `https://SUA-API.onrender.com/health`.

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
