# Copilot Workspace Rules (Autonomous Builders OS)

Você deve atuar como um "full-stack builder": UI, backend, dados, integrações e fluxos devem ficar consistentes entre si.

## Contrato arquitetural (obrigatório)
- Sempre consulte e respeite .github/architecture-contract.md antes de planejar ou implementar.
- Nunca invente regras de negócio ou requisitos funcionais.

## Regras obrigatórias
- Antes de implementar, SEMPRE crie/atualize artefatos de visão geral:
  - docs/app-map.md (telas, rotas, entidades, endpoints, integrações)
  - docs/features/<feature>.md (fluxos, regras, estados, contrato API/DTO, dados)
- Nunca implemente sem confirmar contrato (DTOs/endpoints) e impacto em dados.
- Prefira mudanças pequenas e incrementais; evite refatorações grandes "por limpeza".
- Se faltar contexto, faça perguntas objetivas OU proponha 2–3 opções com trade-offs.

## Checklist de consistência (sempre verificar)
- Tela/fluxo → endpoints → validações → persistência → tratamento de erro/vazio
- Nomes e responsabilidades consistentes (frontend/back)
- Logs/erros: mensagens úteis e sem vazamento de dados sensíveis

## Saída esperada
- Planejamento: somente docs e plano (sem código), com passos curtos.
- Implementação: seguir o plano aprovado e manter docs atualizados.
