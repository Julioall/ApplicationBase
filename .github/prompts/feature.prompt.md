---
name: feature
description: Inicia uma feature com Spec-first + plano e handoff
argument-hint: "Quero a feature X; regras Y; restrições Z"
agent: plan
tools: ['search', 'fetch', 'githubRepo']
---

Crie o planejamento completo da feature informada seguindo o agente `plan`.

Antes de começar, consulte .github/architecture-contract.md.

Entregáveis obrigatórios:
- docs/features/<feature>.md (fluxos, regras, estados, contrato API/DTO, dados)
- docs/plan/<feature>.md (3–6 passos)
- Atualize docs/app-map.md se necessário

Formato do plano (no chat):
## Plan: <título curto>
TL;DR (20–100 palavras)
### Steps (3–6)
1. ...
### Further Considerations (1–3)
1. ...

Inclua no plano o escopo de testes (o que precisa ser validado e por quê).
