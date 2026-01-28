# App Map

## Telas / Rotas
- Explorer de Educação (rota a confirmar) — leitura do cache do Moodle; exibe `LastSyncAt`.
- Exportar Dados da UC (ação/rota existente) — usa cache atual.

## Endpoints (confirmar nomes reais)
- Leitura de categorias/cursos/UCs para Explorer.
- Status de sincronização (`LastSyncAt`).
- Escrita (create/update/import) desativada com ProblemDetails.

## Entidades / Documentos (RavenDB)
- Category/Program (moodle `course_categories`) — campos a confirmar.
- Course (moodle `courses`) — campos a confirmar.
- UcDocument — inclui datas de início/fim e CH.
- SyncStatus — `LastSyncAt` e metadados de invalidação.

## Integrações
- Moodle WebService API (sincronização em background).