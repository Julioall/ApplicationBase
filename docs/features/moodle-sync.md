# Sincronização do Moodle (MoodleSyncHangfireJob)

## Visão Geral
Este documento descreve o funcionamento da sincronização automática de cursos do usuário logado a partir do Moodle, realizada pelo job `MoodleSyncHangfireJob`.

## Fluxo de Sincronização
1. **Autenticação**: O job utiliza o ID do usuário Moodle para buscar o token de acesso (ajuste conforme sua estratégia de autenticação).
2. **Busca de Cursos**: Utiliza o client `IMoodleCourseClient` para consumir o endpoint `core_enrol_get_users_courses` do Moodle, trazendo apenas os cursos do usuário logado.
3. **Mapeamento e Persistência**: Cada curso retornado é mapeado para um `UcDocument` e salvo via `IEducationRepository`.
4. **Recalculo de Períodos**: (Opcional) O job pode recalcular períodos de acordo com a lógica de negócio.
5. **Status e Logging**: O status da sincronização é atualizado e logs são registrados.

## Pontos Importantes
- **Somente leitura**: Apenas endpoints GET do Moodle são utilizados, sem qualquer alteração de dados no Moodle.
- **Token do Usuário**: O método de obtenção do token deve ser adaptado conforme o fluxo de autenticação vigente.
- **Cache**: O RavenDB é utilizado como cache local para reduzir chamadas repetidas ao Moodle.

## Estruturas e Interfaces
- `IMoodleCourseClient`: Interface para buscar cursos do usuário no Moodle.
- `MoodleCourseClient`: Implementação que consome o endpoint REST do Moodle.
- `IMoodleRepository`: Responsável por persistir os dados sincronizados (categorias).
- `IEducationRepository`: Responsável por persistir os cursos (UcDocument).
- `MoodleSyncHangfireJob`: Job Hangfire que orquestra todo o fluxo.

## Exemplo de Uso
O job pode ser disparado manualmente ou agendado, recebendo o ID do usuário Moodle como parâmetro.

---

**Última atualização:** 28/01/2026
