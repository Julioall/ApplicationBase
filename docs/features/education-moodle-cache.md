# Plano: Educação via Cache Moodle

## Camadas afetadas
- Domain (entidades, regras de leitura)
- Infrastructure (RavenDB, integração Moodle)
- Service (casos de uso, sincronização)
- Web/API (endpoints, ProblemDetails)
- Client (UI Explorer e exportação)
- Background (MoodleSyncService)

## Contratos (DTOs/endpoints)
- Confirmar endpoints atuais de leitura do Explorer/Exportar.
- Definir/confirmar endpoint de status de sincronização (`LastSyncAt`).
- Desativar endpoints de escrita (create/edit/import) retornando ProblemDetails.

## Impacto em dados
- RavenDB passa a ser cache: documentos de Category/Program, Course, UcDocument.
- Criar documento de status de sync com `LastSyncAt` e metadados de invalidação/TTL.

## Validações e erros
- Bloquear escrita via API/UI.
- Erros padronizados com ProblemDetails (RFC 7807).

## i18n
- Remover textos e botões de criação/edição/importação.
- Incluir mensagens para leitura do Moodle e status de sync.

## Escopo de testes
- Serviços: mapeamento Moodle → cache, persistência e invalidação.
- API: endpoints de leitura, bloqueio de escrita, status de sync.
- UI: ausência de botões de escrita, Explorer lendo cache, exportação funcionando.
- Background: execução e atualização de `LastSyncAt`.

## Steps
1. Inventariar modelos atuais (Course/Program/UcDocument), endpoints e UI do Explorer/Exportar.
2. Especificar contrato de dados/DTOs e status de sync em docs/features.
3. Planejar backend: refatoração de domínio, sync em background, cache + `LastSyncAt`, bloqueio de escrita.
4. Planejar frontend: remoção de botões/diálogos, leitura do cache, exibição de status e manutenção do export.
5. Definir e priorizar o pacote de testes automatizados.