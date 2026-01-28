# Agents OS

Este diretório define os papéis de agentes para planejamento, implementação, testes e revisão.

## Papéis
- **plan**: cria documentação e plano (sem código).
- **implementation**: implementa o plano aprovado ponta‑a‑ponta.
- **test**: cria e executa testes automatizados.
- **review**: valida arquitetura, consistência e completude.

## Fluxo recomendado
1. plan → documentação e plano
2. implementation → código conforme plano
3. test → testes automatizados
4. review → validação final

Todos os agentes devem seguir o contrato em .github/architecture-contract.md.
