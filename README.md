# ReceiptGenerator

Projeto .NET 9 para emissao de recibos em PDF, organizado em camadas com orientacao a DDD.

## Estrutura

- `src/ReceiptGenerator.Domain`: entidades, regras basicas de negocio e contratos de repositorio.
- `src/ReceiptGenerator.Application`: casos de uso, DTOs e abstracoes para infraestrutura.
- `src/ReceiptGenerator.Infrastructure`: EF Core, PostgreSQL, repositories, JWT, BCrypt e QuestPDF.
- `src/ReceiptGenerator.Api`: controllers HTTP, autenticacao, CORS e composicao de dependencias.
- `web`: frontend Angular para emissao e consulta de recibos.

## Fluxo principal

1. Registrar ou autenticar um usuario.
2. Cadastrar clientes do usuario autenticado.
3. Emitir recibos vinculados a um cliente.
4. Baixar o recibo em PDF pelo endpoint `GET /api/receipts/{id}/pdf`.

## Endpoints

- `POST /api/auth/register`
- `POST /api/auth/login`
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

Atualize `src/ReceiptGenerator.Api/appsettings.json` com a conexao PostgreSQL e substitua `JwtSettings:Secret` por um segredo seguro.

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
