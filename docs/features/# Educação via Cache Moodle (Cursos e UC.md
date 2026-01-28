# Educação via Cache Moodle (Cursos e UCs)

## Contexto
O sistema deixa de importar dados manualmente via Excel e passa a atuar como camada de visualização/inteligência sobre o Moodle. O RavenDB vira cache de alta performance para consultas e indicadores.

## Objetivos
- Espelhar a hierarquia do Moodle (Categorias → Cursos → UCs).
- Tornar Educação **somente leitura** via UI/API.
- Sincronizar Moodle → RavenDB em background.
- Suportar campos de OKR (datas início/fim e carga horária/CH).
- Manter exportação de UC baseada no cache.

## Não‑objetivos
- Criar/editar cursos/UCs manualmente.
- Definir novas regras de negócio além do solicitado.

## Fluxos Principais
1. **Explorer de Educação (UI)**
   - Listar categorias → cursos → UCs a partir do cache.
   - Exibir `LastSyncAt`.
2. **Exportar Dados da UC**
   - Gera relatório usando os dados do cache atual.
3. **Sincronização (Background)**
   - `MoodleSyncService` consulta APIs do Moodle e atualiza cache no RavenDB.

## Regras e Restrições
- Nenhum dado de curso/UC pode ser criado ou editado manualmente.
- Toda leitura deve usar o cache no RavenDB.
- Cache deve ter `LastSyncAt` e mecanismo de invalidação.

## Contrato API/DTO (a confirmar nos endpoints atuais)
- Endpoints de leitura para Explorer e Export permanecem, consumindo cache.
- Endpoints de escrita (create/update/import) devem ser desativados e retornar ProblemDetails.
- Se não existir, definir endpoint de status de sync (ex.: `LastSyncAt`).

## Modelo de Dados (RavenDB – Cache)
- **Category/Program**: refletir `course_categories` do Moodle (campos a confirmar).
- **Course**: refletir `courses` do Moodle (campos a confirmar).
- **UcDocument**: derivado de Course/Category com campos necessários para OKR:
  - `StartDate`, `EndDate`, `WorkloadHours` (CH).
- **SyncStatus** (novo documento): `LastSyncAt`, `LastSyncStatus`, metadados de invalidação/TTL.

## Validação e Erros
- Bloquear operações de escrita com ProblemDetails (RFC 7807).
- Manter mensagens úteis sem dados sensíveis.

## i18n
- Remover textos de criação/edição/importação.
- Ajustar labels para indicar leitura do Moodle e status de sincronização.

## Pendências de Confirmação
- Endpoints e DTOs atuais de Educação/Explorer/Exportar.
- Estrutura exata dos documentos RavenDB existentes (Course/Program/UcDocument).
- Campos do Moodle necessários para OKR (além de datas e CH).