# Melhorias planejadas — Coopertáxi Recibos

Backlog priorizado de melhorias para o sistema de emissão de recibos.
Ordenado por prioridade dentro de cada grupo.

---

## Alta prioridade

### 1. Edição de perfil pelo próprio motorista
**Endpoint:** `PUT /api/users/me`

Permite que o motorista edite os próprios dados sem depender do administrador.

Campos editáveis pelo motorista:
- `FullName` — nome exibido nos recibos futuros; não afeta recibos já emitidos
- `Password` — exige confirmação da senha atual via BCrypt antes de aceitar

Campos **não** editáveis pelo próprio motorista (somente admin):
- `Username` — identificador de login único
- `Role`, `IsActive` — controle administrativo

Regras de implementação:
- Ao trocar senha: invalidar o refresh token atual para forçar novo login
- Ao trocar `FullName`: informar na UI que a mudança reflete no próximo login (JWT carrega `fullName`)
- Retornar `403` se o usuário tentar editar outro perfil via `id` diferente do próprio

---

### 2. Ícones do PWA/APK
O `manifest.webmanifest` já referencia os arquivos, mas eles não existem ainda.

Gerar a partir do logo da cooperativa:
- `assets/icons/icon-72x72.png`
- `assets/icons/icon-96x96.png`
- `assets/icons/icon-128x128.png`
- `assets/icons/icon-144x144.png`
- `assets/icons/icon-152x152.png`
- `assets/icons/icon-192x192.png`
- `assets/icons/icon-384x384.png`
- `assets/icons/icon-512x512.png`

Ferramentas sugeridas: [PWA Asset Generator](https://github.com/elegantapp/pwa-asset-generator), [RealFaviconGenerator](https://realfavicongenerator.net/) ou qualquer editor de imagem.

---

### 3. Edição de clientes na interface
O endpoint `PUT /api/clients/{id}` já existe no backend, mas a UI não expõe formulário de edição.

O que adicionar:
- Botão "Editar" em cada item da lista de clientes
- Formulário para editar `name`, `address`, `taxId`
- Validação: nome não pode ser vazio

Impacto: nenhum nos recibos já emitidos (o recibo guarda snapshot do cliente).

---

### 4. Filtro de recibos por período
Adicionar filtro de mês/ano na listagem de recibos.

Backend:
- Parâmetros opcionais `month` e `year` em `GET /api/receipts`
- Aplicar filtro em `ReceiptService.GetPagedAsync()`

Frontend:
- Seletores de mês/ano acima da lista
- Ao alterar: resetar para página 1 e recarregar

---

## Média prioridade

### 5. Dados do emissor pré-preenchidos do perfil
Hoje o motorista digita telefone e e-mail toda vez que usa os "horários extras".

O que fazer:
- Adicionar campos `Phone` e `Email` ao cadastro de `User`
- Migration para adicionar as colunas
- No formulário de recibo: pré-preencher `IssuerPhone` e `IssuerEmail` do perfil logado
- O motorista pode sobrescrever antes de salvar

---

### 6. Relatório disponível para o próprio motorista
Hoje `GET /api/reports/monthly-summary` retorna dados apenas para admins.

Ajuste:
- Se o usuário for `Driver`, retornar o relatório filtrado pelos próprios recibos
- Sem acesso aos dados de outros motoristas
- Exibir na UI do motorista: total do mês, número de corridas, média por corrida

---

### 7. Soft delete de recibos (cancelamento)
Substituir o `DELETE` físico por cancelamento lógico.

Schema:
```sql
ALTER TABLE "Receipts" ADD COLUMN "CancelledAt" TIMESTAMP;
ALTER TABLE "Receipts" ADD COLUMN "CancelReason" VARCHAR(500);
```

Comportamento:
- `DELETE /api/receipts/{id}` passa a setar `CancelledAt = now()` em vez de remover a linha
- Recibos cancelados não aparecem na listagem padrão
- PDF de recibo cancelado exibe marca "CANCELADO" em destaque (vermelho diagonal)
- Admin pode visualizar recibos cancelados com filtro opcional

---

### 8. Reset de senha pelo administrador
**Endpoint:** `PUT /api/users/{id}/reset-password`

Somente `SystemAdmin` e `CoopAdmin` podem usar.
- Não exige confirmação da senha atual do usuário alvo
- Invalida o refresh token do usuário alvo (força novo login)
- Corpo: `{ "newPassword": "..." }`

---

### 9. Campo `UpdatedAt` na entidade `User`
Migration simples para adicionar timestamp de última modificação.

```sql
ALTER TABLE "Users" ADD COLUMN "UpdatedAt" TIMESTAMP;
```

- Atualizado automaticamente em qualquer `UPDATE` via EF Core (`SaveChangesInterceptor` ou override de `SaveChanges`)
- Base para auditoria futura sem infraestrutura adicional agora

---

## Baixa prioridade

### 10. Exportação do relatório (CSV/Excel)
Botão "Exportar" no relatório mensal para download em CSV.

Backend:
- `GET /api/reports/monthly-summary/export?month=&year=&format=csv`
- Gerar CSV com `CsvHelper` ou manualmente com `StringBuilder`

Frontend:
- Botão que chama o endpoint e faz download do arquivo

---

### 11. Recuperação de senha por e-mail
Fluxo de "esqueci minha senha" sem depender do administrador.

Requer:
- Configuração de SMTP (SendGrid, Mailgun, ou SMTP próprio) em `appsettings.json`
- Tabela `PasswordResetTokens` (token, userId, expiresAt, usedAt)
- Endpoints: `POST /api/auth/forgot-password` e `POST /api/auth/reset-password`
- E-mail com link contendo token assinado (HMAC ou GUID único)

Complexidade: alta. Só vale se os motoristas perderem senha com frequência.

---

### 12. Paginação server-side da lista de clientes
Hoje `GET /api/clients` retorna todos os clientes em memória.

Quando implementar: somente se a lista crescer a ponto de causar lentidão percebida.

- Adicionar parâmetros `search`, `page`, `pageSize`
- Frontend: campo de busca com debounce em vez de `<datalist>`

---

### 13. Histórico de alterações de perfil (auditoria completa)
Tabela `UserAuditLogs` com: `UserId`, `Field`, `OldValue`, `NewValue`, `ChangedBy`, `ChangedAt`.

Pré-requisito: implementar o item 9 (`UpdatedAt`) primeiro.
Implementar somente se houver necessidade real de rastreabilidade (ex.: disputas internas na cooperativa).

---

## Notas técnicas gerais

- Todos os novos endpoints devem seguir o padrão existente: Clean Architecture, Application layer com `ICommand`/`IQuery`, validação no domínio
- Migrations devem ser geradas com `dotnet ef migrations add NomeDaMigration -p src/ReceiptGenerator.Infrastructure -s src/ReceiptGenerator.Api`
- Testes manuais no Swagger antes de integrar com o frontend
- `Receipt.DriverName` é e deve continuar sendo um snapshot imutável — nenhuma melhoria deve alterar recibos já emitidos
