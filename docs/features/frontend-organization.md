# Organização e Melhoria do Frontend Angular

## 📋 Resumo Executivo

Este documento descreve a reorganização estrutural do frontend Angular 18 para melhorar manutenibilidade, performance e reutilização de código.

**Período:** Janeiro 2026  
**Status:** ✅ Implementado

---

## 🏗️ Arquitetura Nova

### Estrutura de Pastas

```
src/app/
├── core/                          # Funcionalidades globais
│   ├── services/                  # Serviços centralizados
│   ├── interceptors/              # HTTP Interceptors
│   └── guards/                    # Route Guards
├── shared/                        # Código compartilhado
│   ├── components/                # Componentes reutilizáveis (Button, Card, Alert, Badge)
│   ├── base-components/           # Componentes base (Smart, Dumb)
│   ├── directives/                # Diretivas reutilizáveis
│   ├── pipes/                     # Pipes personalizados
│   └── validators/                # Validadores compartilhados
├── features/                      # Módulos de features
│   ├── todo/                      # Feature: Tarefas
│   │   ├── containers/            # Smart Components
│   │   ├── components/            # Dumb Components
│   │   ├── services/              # Serviços específicos (Façade)
│   │   └── models/                # Interfaces/Tipos
│   └── auth/                      # Feature: Autenticação
├── layouts/                       # Layouts de página (header, sidebar)
├── models/                        # Modelos globais
├── app.component.ts               # Componente raiz
├── app-routing.module.ts          # Routing principal
└── app.module.ts                  # Módulo principal
```

### Princípios de Design

#### 1. **Smart vs Dumb Components**

**Smart Components (Containers):**
- Localização: `features/<feature>/containers/`
- Responsabilidades:
  - Buscar dados via serviços
  - Gerenciar estado
  - Orquestrar sub-componentes
  - Tratar erros e loading
- Base: `BaseSmartComponent`

Exemplo:
```typescript
@Component({...})
export class TodoBoardComponent extends BaseSmartComponent {
  todos$ = this.todoFacade.todos;
  loading$ = this.todoFacade.loading;
  
  constructor(private todoFacade: TodoFacadeService) { super(); }
  
  ngOnInit(): void {
    this.todoFacade.loadTodos();
  }
}
```

**Dumb Components (Presentational):**
- Localização: `features/<feature>/components/`
- Responsabilidades:
  - Apenas exibir dados via `@Input`
  - Emitir eventos via `@Output`
  - Sem lógica de negócio
  - Sem chamadas à API
- Base: `BasePresentationalComponent` (com `ChangeDetectionStrategy.OnPush`)

Exemplo:
```typescript
@Component({..., changeDetection: ChangeDetectionStrategy.OnPush})
export class TodoTaskDetailComponent extends BasePresentationalComponent {
  @Input() todo!: TodoModel;
  @Output() onEdit = new EventEmitter<TodoModel>();
}
```

#### 2. **Serviços Façade**

Padrão para orquestração de múltiplos serviços:

```typescript
@Injectable()
export class TodoFacadeService {
  todos$ = this.http.get<Todo[]>('/api/todos');
  loading$ = new BehaviorSubject(false);
  
  loadTodos(): void {
    this.loading$.next(true);
    // Chamadas a múltiplos serviços
  }
}
```

#### 3. **Design Tokens com Tailwind CSS**

Todos os tokens de design estão centralizados em `tailwind.config.js` para garantir **consistência única** em todo o projeto:

**Categorias de Tokens:**
- **Cores:** Primary (azul), Secondary (roxo), Success, Warning, Danger, Info, Gray
- **Tipografia:** Font families, sizes (xs-5xl), weights (light-extrabold)
- **Espaçamento:** Sistema 4px (0-40)
- **Sombras:** Elevation levels (xs-2xl) com aliases semânticos
- **Border Radius:** Complete scale para rounded corners
- **Z-Index:** Layering system semântico
- **Transitions:** Durations e timing functions

Uso em componentes (Tailwind utility classes):
```typescript
@Component({
  template: `
    <button class="bg-primary-600 text-white hover:bg-primary-700 px-4 py-2 rounded-lg transition-all duration-150">
      Click me
    </button>
  `
})
export class ExampleComponent {}
```

**Vantagens:**
✅ Single source of truth (tailwind.config.js)
✅ Sem duplicação de estilos SCSS
✅ Consistência garantida
✅ Type-safe com autocomplete do IDE

---

## 🎨 Design System

### Componentes Base

#### Button
```html
<app-button 
  variant="primary" 
  size="md" 
  [loading]="isLoading"
  (onClick)="save()">
  Salvar
</app-button>
```

Props: `variant` (primary|secondary|danger|ghost), `size` (sm|md|lg), `disabled`, `loading`

#### Card
```html
<app-card [elevation]="2" [hoverable]="true">
  <h3>Card Title</h3>
  <p>Card content</p>
</app-card>
```

Props: `elevation` (1|2|3), `flat`, `hoverable`

#### Alert
```html
<app-alert type="success" [dismissible]="true">
  Operação concluída!
</app-alert>
```

Props: `type` (info|success|warning|error), `dismissible`

#### Badge
```html
<app-badge variant="success" size="md">
  Ativo
</app-badge>
```

Props: `variant`, `size` (sm|md|lg), `outline`

---

## 🧪 Testes

### Estratégia de Testes

1. **Testes Unitários** (componentes, serviços)
   - Framework: Jasmine/Karma
   - Localização: `*.spec.ts`
   - Cobertura mínima: 80%

2. **Testes E2E** (fluxos críticos)
   - Framework: Playwright
   - Localização: `e2e/tests/`
   - Padrão: Page Objects

### Executar Testes

```bash
# Testes unitários
npm test

# Testes E2E
npm run e2e

# Com cobertura
npm test -- --coverage
```

---

## 📦 Componentes Implementados

### Smart Components
- ✅ `TodoBoardComponent` - Gestor de tarefas (containers)

### Dumb Components
- ✅ `TodoTaskDetailComponent` - Detalhe de tarefa (components)

### Componentes Reutilizáveis (UI)
- ✅ `ButtonComponent` - Botão polivalente
- ✅ `CardComponent` - Container com estilo
- ✅ `AlertComponent` - Alertas e notificações
- ✅ `BadgeComponent` - Etiquetas e status

### Design Tokens
- ✅ Cores (50 variações)
- ✅ Tipografia (escalas e mixins)
- ✅ Espaçamento (4px a 160px)
- ✅ Sombras (elevation levels)
- ✅ Breakpoints (responsivos)

---

## 🔧 Padrões & Boas Práticas

### Change Detection Strategy

Componentes Dumb usam `ChangeDetectionStrategy.OnPush`:
```typescript
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush
})
```

**Benefícios:**
- ✅ Performance melhorada
- ✅ Fewer change detection cycles
- ✅ Imutabilidade incentivada

### Gerenciamento de Inscrições

Use `BaseSmartComponent` com `destroy$`:
```typescript
export class MyComponent extends BaseSmartComponent {
  constructor(private api: ApiService) { super(); }
  
  ngOnInit(): void {
    this.api.getData()
      .pipe(takeUntil(this.destroy$))
      .subscribe(...);
  }
}
```

### Typing Forte

Sempre use interfaces:
```typescript
export interface TodoModel {
  id: string;
  title: string;
  completed: boolean;
  priority: 'low' | 'medium' | 'high';
}
```

---

## 📖 Guia de Uso

### Criar Novo Componente Dumb

1. Criar pasta: `features/<feature>/components/<name>/`
2. Criar arquivo: `<name>.component.ts`
3. Estender `BasePresentationalComponent`
4. Usar `@Input/@Output` para comunicação
5. Adicionar testes: `<name>.component.spec.ts`

Exemplo:
```typescript
import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { BasePresentationalComponent } from '@shared/base-components';

@Component({
  selector: 'app-todo-item',
  standalone: true,
  template: `<div>{{ item.title }}</div>`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodoItemComponent extends BasePresentationalComponent {
  @Input() item!: TodoModel;
  @Output() onSelect = new EventEmitter<TodoModel>();
}
```

### Criar Novo Smart Component

1. Criar pasta: `features/<feature>/containers/<name>/`
2. Criar arquivo: `<name>.component.ts`
3. Estender `BaseSmartComponent`
4. Injetar serviços (Façade)
5. Usar observables em template com `| async`

Exemplo:
```typescript
import { Component, OnInit } from '@angular/core';
import { BaseSmartComponent } from '@shared/base-components';

@Component({
  selector: 'app-todo-list',
  standalone: true,
  template: `
    <div *ngFor="let item of (todos$ | async) as todos">
      {{ item.title }}
    </div>
  `
})
export class TodoListComponent extends BaseSmartComponent implements OnInit {
  todos$ = this.todoFacade.todos;

  constructor(private todoFacade: TodoFacadeService) { super(); }

  ngOnInit(): void {
    this.todoFacade.loadTodos();
  }
}
```

---

## ✅ Checklist de Qualidade

- ✅ Componentes compilam sem erros
- ✅ Testes unitários passando (80%+ cobertura)
- ✅ Testes E2E cobrindo fluxos críticos
- ✅ Design System tokens implementados
- ✅ Componentes reutilizáveis criados
- ✅ Smart/Dumb separation respeitada
- ✅ Change Detection otimizado
- ✅ Acessibilidade (ARIA labels)
- ✅ Responsive design (Tailwind)
- ✅ Documentação atualizada

---

## 📚 Recursos

- [Angular Best Practices](https://angular.io/guide/styleguide)
- [Design Systems](https://www.designsystems.com/)
- [Playwright Testing](https://playwright.dev/)
- [RxJS Patterns](https://rxjs.dev/)

---

## 📝 Notas

- Todos os componentes são **standalone** (Angular 18+)
- Use **Tailwind CSS** para estilos
- Use **RxJS** para reatividade
- Use **TypeScript** com tipagem forte

---

**Versão:** 1.0  
**Data:** 30 de Janeiro de 2026  
**Responsável:** Feature Agent
