# Contrato Arquitetural (Obrigatório)

Este contrato define **como** construir software neste repositório. Ele não define regras de negócio.
Todos os agentes devem ler e respeitar este documento antes de planejar, implementar, testar ou revisar.

## Stack

### Backend
- ASP.NET Core 8
- Clean / Onion Architecture
- FluentValidation
- ProblemDetails (RFC 7807)
- JWT Authentication
- RavenDB (document database / cache)

### Frontend
- Angular 18
- Tailwind CSS
- ngx-translate (i18n)
- Interceptors para loading e error handling

## Princípios Obrigatórios
- Não inventar regras de negócio ou requisitos funcionais.
- Implementar soluções completas ponta‑a‑ponta quando aplicável (API + UI + dados).
- Respeitar fronteiras arquiteturais e camadas (não cruzar dependências indevidas).
- Preferir mudanças pequenas e incrementais.
- Documentar contratos e impactos antes de implementar.

## Validações e Erros
- Usar FluentValidation para validações de entrada.
- Padronizar erros com ProblemDetails (RFC 7807).
- Garantir mensagens úteis sem expor dados sensíveis.

## Integração e Dados
- Toda mudança de contrato deve atualizar DTOs, endpoints e documentação.
- Considerar persistência e cache em RavenDB quando pertinente.
- Garantir consistência entre API, UI e modelos de domínio.
- Sincronizações ou workloads pesados contra o Moodle devem ser delegados a processos em background para não sobrecarregar o pipeline HTTP, usando caches locais (RavenDB) e filas internas para ações realizadas e dados derivados.
- Quando necessário, priorizar serviços de background (por exemplo, `BackgroundService` do ASP.NET Core) para reunir dados do Moodle, aplicar transformações e invalidar cache sem aumentar latência percebida.
- Avaliar a introdução de filas (ex.: RabbitMQ) para orquestrar cargas assíncronas e desacoplar consumidores quando sincronizações com Moodle ficarem mais complexas.

## Frontend
- Manter i18n via ngx-translate.
- Usar interceptors para loading e erros globais.
- Evitar lógica de negócio no frontend; focar em orquestração e UX.

## Testes
- Testes automatizados são obrigatórios para serviços/casos de uso críticos, endpoints e fluxos críticos de UI.
- Testes devem validar integração e comportamento esperado sem reimplementar regras de negócio.
