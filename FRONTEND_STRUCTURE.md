# 🚀 Frontend - Estrutura Reorganizada

```
Application.Client/
├── src/
│   └── app/
│       ├── core/                           # Camada de Infraestrutura
│       │   ├── services/                   # Serviços globais (auth, http, etc)
│       │   ├── interceptors/               # HTTP Interceptors
│       │   └── guards/                     # Route Guards
│       │
│       ├── features/                       # Features/Módulos da Aplicação
│       │   ├── todo/                       # Feature: Tarefas
│       │   │   ├── containers/             # Smart Components
│       │   │   │   ├── todo-board.component.ts
│       │   │   │   ├── todo-board.component.spec.ts
│       │   │   │   ├── todo-board.component.html
│       │   │   │   └── todo-board.component.scss
│       │   │   │
│       │   │   ├── components/             # Dumb Components
│       │   │   │   ├── todo-task-detail.component.ts
│       │   │   │   ├── todo-task-detail.component.spec.ts
│       │   │   │   ├── todo-task-detail.component.html
│       │   │   │   └── todo-task-detail.component.scss
│       │   │   │
│       │   │   ├── services/               # Feature Services (Façade)
│       │   │   │   └── todo-facade.service.ts
│       │   │   │
│       │   │   └── models/                 # Tipos/Interfaces
│       │   │       └── todo.model.ts
│       │   │
│       │   └── auth/                       # Feature: Autenticação
│       │       ├── containers/
│       │       ├── components/
│       │       ├── services/
│       │       └── models/
│       │
│       ├── shared/                         # Código Compartilhado
│       │   ├── components/                 # Componentes de UI Reutilizáveis
│       │   │   ├── button/
│       │   │   │   └── button.component.ts
│       │   │   ├── card/
│       │   │   │   └── card.component.ts
│       │   │   ├── alert/
│       │   │   │   └── alert.component.ts
│       │   │   ├── badge/
│       │   │   │   └── badge.component.ts
│       │   │   └── index.ts
│       │   │
│       │   ├── base-components/           # Classes Base para Componentes
│       │   │   ├── base-smart.component.ts
│       │   │   ├── base-presentational.component.ts
│       │   │   └── index.ts
│       │   │
│       │   ├── design-tokens/             # Design Tokens (SCSS)
│       │   │   ├── colors.scss            # Paleta de cores
│       │   │   ├── typography.scss        # Escalas tipográficas
│       │   │   ├── spacing.scss           # Sistema de espaçamento
│       │   │   ├── shadows.scss           # Elevation levels
│       │   │   ├── breakpoints.scss       # Pontos responsivos
│       │   │   └── index.scss             # Index
│       │   │
│       │   ├── directives/                # Diretivas Reutilizáveis
│       │   ├── pipes/                     # Pipes Customizados
│       │   ├── validators/                # Validadores
│       │   ├── modal/                     # Serviço de Modal
│       │   ├── notification/              # Serviço de Notificações
│       │   ├── navbar/                    # Componente Navbar
│       │   ├── storage/                   # Serviços de Armazenamento
│       │   ├── README.md                  # Documentação do Shared
│       │   └── index.ts
│       │
│       ├── layouts/                       # Layouts de Página
│       │   ├── main-layout.component.ts
│       │   └── auth-layout.component.ts
│       │
│       ├── models/                        # Modelos Globais
│       ├── app.component.ts               # Componente Raiz
│       ├── app-routing.module.ts          # Routing Principal
│       └── app.module.ts                  # Módulo Principal
│
├── e2e/                                   # Testes E2E
│   ├── tests/
│   │   ├── todo.spec.ts                   # Testes Playwright
│   │   └── ...
│   ├── playwright.config.ts
│   └── package.json
│
├── docs/
│   └── features/
│       └── frontend-organization.md       # Documentação Completa
│
├── public/
├── src/
│   ├── index.html
│   ├── styles.scss                        # Estilos Globais
│   ├── main.ts
│   └── ...
│
├── angular.json
├── package.json                           # Dependencies
├── tsconfig.json                          # TypeScript Config
├── tailwind.config.js                     # Tailwind CSS
└── README.md
```

---

## 📦 Dependências e Estrutura

### Versões Principais
```json
{
  "@angular/core": "^18.2.5",
  "@angular/cli": "^18.2.5",
  "typescript": "~5.5.0",
  "rxjs": "~7.8.0",
  "tailwindcss": "^3.x"
}
```

### Padrões Utilizados

#### 1️⃣ Smart vs Dumb Components
```
Containers (Smart)           Components (Dumb)
├─ Fetching dados            ├─ @Input/@Output
├─ State management          ├─ No business logic
├─ API calls                 ├─ OnPush strategy
└─ Orquestra sub-componentes └─ Reusable
```

#### 2️⃣ Serviço Façade
```
API Service
    ↓
Façade Service (Orquestração)
    ↓
Smart Component (Consumo)
```

#### 3️⃣ Design Tokens
```
colors.scss     → Paleta
typography.scss → Tipos
spacing.scss    → Espaçamento
shadows.scss    → Elevação
breakpoints.scss→ Responsive
```

---

## 🧩 Componentes Disponíveis

### UI Components (Reutilizáveis)

#### Button
```html
<app-button 
  variant="primary|secondary|danger|ghost" 
  size="sm|md|lg"
  [loading]="false"
  (buttonClick)="handleClick()">
  Click me
</app-button>
```

#### Card
```html
<app-card [elevation]="2" [hoverable]="true">
  <h3>Title</h3>
  <p>Content</p>
</app-card>
```

#### Alert
```html
<app-alert 
  type="info|success|warning|error" 
  [dismissible]="true">
  Message
</app-alert>
```

#### Badge
```html
<app-badge 
  variant="success" 
  size="md" 
  [outline]="false">
  Label
</app-badge>
```

---

## 🎨 Design Tokens

### Cores Disponíveis
```scss
// Primárias
$primary-50 até $primary-900

// Secundárias
$secondary-50 até $secondary-900

// Semânticas
$success-*, $warning-*, $error-*
$gray-*, $color-text-primary, etc
```

### Tipografia
```scss
// Sizes: xs, sm, base, lg, xl, 2xl, 3xl, 4xl, 5xl
// Weights: light, normal, medium, semibold, bold, extrabold
// Mixins: @include h1-style, h2-style, body-style, etc

@include h1-style;    // Aplica estilo h1
@include body-style;  // Aplica estilo body
```

### Espaçamento
```scss
// Scale: 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 14, 16, 20, 24, 28, 32, 36, 40
$spacing-2: 0.5rem;   // 8px
$spacing-4: 1rem;     // 16px (padrão)
$spacing-6: 1.5rem;   // 24px

// Aliases
$spacing-xs, $spacing-sm, $spacing-md, $spacing-lg, $spacing-xl, $spacing-2xl
```

### Sombras (Elevation)
```scss
$elevation-1  → xs (hover cards)
$elevation-2  → sm (cards)
$elevation-3  → md (floating)
$elevation-4  → lg (modals)
$elevation-5  → xl (important modals)
$elevation-6  → 2xl (top overlays)

@include elevation(2);  // Aplica elevation-2
```

### Breakpoints
```scss
$breakpoint-xs: 320px   → mobile
$breakpoint-sm: 640px   → small tablet
$breakpoint-md: 768px   → tablet
$breakpoint-lg: 1024px  → desktop
$breakpoint-xl: 1280px  → wide desktop
$breakpoint-2xl: 1536px → ultra-wide

@include media-lg { /* CSS */ }  // >= 1024px
```

---

## 📋 Checklist de Uso

### Criar Novo Componente Dumb
- [ ] Criar `features/<feature>/components/<name>/`
- [ ] Estender `BasePresentationalComponent`
- [ ] Usar `ChangeDetectionStrategy.OnPush`
- [ ] Usar `@Input/@Output` para comunicação
- [ ] Criar arquivo `.spec.ts`
- [ ] Importar via `index.ts`

### Criar Novo Componente Smart
- [ ] Criar `features/<feature>/containers/<name>/`
- [ ] Estender `BaseSmartComponent`
- [ ] Injetar Façade service
- [ ] Usar `observable$ | async` no template
- [ ] Implementar `ngOnInit`
- [ ] Criar arquivo `.spec.ts`

### Usar Design Tokens
- [ ] Importar: `@import '@shared/design-tokens';`
- [ ] Usar cores: `color: $primary-600;`
- [ ] Usar spacing: `padding: $spacing-4;`
- [ ] Usar sombras: `@include elevation(2);`
- [ ] Usar breakpoints: `@include media-lg { /* CSS */ }`

---

## 🚀 Como Rodar

```bash
# Instalar dependências
npm install

# Desenvolvimento
npm start              # Inicia servidor dev (localhost:4200)

# Build
npm run build          # Production build

# Testes
npm test              # Testes unitários (Jasmine)
npm run e2e           # Testes E2E (Playwright)

# Linting
npm run lint          # Validar código (ESLint)
```

---

## 📚 Documentação Disponível

1. **`docs/features/frontend-organization.md`**
   - Arquitetura completa
   - Padrões de design
   - Guia de uso

2. **`src/app/shared/README.md`**
   - Design tokens
   - Componentes
   - Boas práticas

3. **`FRONTEND_ORGANIZATION_REPORT.md`**
   - Relatório executivo
   - Arquivos criados
   - Validações realizadas

---

## ✅ Status

| Item | Status |
|------|--------|
| Design System | ✅ Implementado |
| Componentes Reutilizáveis | ✅ Implementado |
| Arquitetura Smart/Dumb | ✅ Implementado |
| Testes Unitários | ✅ Implementado |
| Testes E2E | ✅ Implementado |
| Documentação | ✅ Completa |
| ESLint | ✅ 100% Passando |
| Ready to Deploy | ✅ Sim |

---

**Versão:** 1.0  
**Data:** 30 de Janeiro de 2026  
**Pronto para:** Production ✨
