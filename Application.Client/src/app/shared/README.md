# Shared Components & Design Tokens

Módulo compartilhado contendo componentes reutilizáveis, design tokens (via Tailwind) e utilitários.

## 📦 Estrutura

```
shared/
├── components/              # Componentes de UI reutilizáveis (Tailwind CSS)
│   ├── button/             # Button component
│   ├── card/               # Card component
│   ├── alert/              # Alert component
│   ├── badge/              # Badge component
│   └── index.ts            # Exports
├── base-components/        # Classes base para componentes
│   ├── base-smart.component.ts
│   ├── base-presentational.component.ts
│   └── index.ts
├── directives/             # Diretivas reutilizáveis
├── pipes/                  # Pipes personalizados
├── validators/             # Validadores customizados
├── storage/                # Serviços de armazenamento
├── notification/           # Serviços de notificação
├── modal/                  # Serviços de modal
└── navbar/                 # Componentes de navegação
```

---

## 🎨 Design Tokens (Tailwind Config)

Todos os design tokens estão centralizados em `tailwind.config.js` para garantir consistência em todo o projeto.

### Cores

```js
// tailwind.config.js - Paleta de cores
colors: {
  // Cores primárias
  primary: { 50: '...', 100: '...', ..., 900: '#003366' }    // Azul
  secondary: { 50: '...', ..., 900: '#4c0099' }              // Roxo
  
  // Cores semânticas
  success: { 50: '...', ..., 600: '#16a34a' }    // Verde
  warning: { 50: '...', ..., 600: '#d97706' }    // Laranja
  danger: { 50: '...', ..., 600: '#dc2626' }     // Vermelho
  info: { 50: '...', ..., 600: '#0284c7' }       // Azul-info
  
  // Cores neutras
  gray: { 50: '#f9fafb', ..., 900: '#111827' }
  
  // Cores semânticas por contexto
  surface: '#ffffff'      // Background de cards
  text: '#111827'         // Texto principal
  border: '#e5e7eb'       // Bordas
  overlay: '#000000'      // Sobreposições
}
```

### Tipografia

```js
// tailwind.config.js - Tipografia
fontSize: {
  xs: ['0.75rem', { lineHeight: '1rem' }],      // 12px
  sm: ['0.875rem', { lineHeight: '1.25rem' }],  // 14px
  base: ['1rem', { lineHeight: '1.5rem' }],     // 16px
  lg: ['1.125rem', { lineHeight: '1.75rem' }],  // 18px
  xl: ['1.25rem', { lineHeight: '1.75rem' }],   // 20px
  // ... até 5xl
}

fontWeight: {
  light: 300,
  normal: 400,
  medium: 500,
  semibold: 600,
  bold: 700,
  extrabold: 800
}
```

### Espaçamento

```js
// tailwind.config.js - Escala de espaçamento (4px base)
spacing: {
  0: '0',
  1: '0.25rem',   // 4px
  2: '0.5rem',    // 8px
  3: '0.75rem',   // 12px
  4: '1rem',      // 16px
  // ... até 40: '10rem' (160px)
}
```

### Sombras

```js
// tailwind.config.js - Elevation system
boxShadow: {
  xs: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
  sm: '0 1px 3px 0 rgba(0, 0, 0, 0.1)',
  md: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
  lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
  xl: '0 20px 25px -5px rgba(0, 0, 0, 0.1)',
  // Aliases semânticos
  elevation1: '... (xs)',
  elevation2: '... (sm)',
  elevation3: '... (md)'
}
```

### Breakpoints

```scss
$breakpoint-sm: 640px;   // Tablets
$breakpoint-md: 768px;   // Small laptops
$breakpoint-lg: 1024px;  // Laptops
$breakpoint-xl: 1280px;  // Desktops

// Media queries
@include media-lg {
  // CSS que aplica em screens >= 1024px
}
```

---

## 🧩 Componentes Reutilizáveis

### Button

**Uso:**
```html
<app-button 
  variant="primary" 
  size="md" 
  [disabled]="false"
  [loading]="isSubmitting"
  (onClick)="handleClick()">
  Click me
</app-button>
```

**Props:**
- `variant`: 'primary' | 'secondary' | 'danger' | 'ghost' (padrão: 'primary')
- `size`: 'sm' | 'md' | 'lg' (padrão: 'md')
- `disabled`: boolean (padrão: false)
- `loading`: boolean (padrão: false)
- `ariaLabel`: string (acessibilidade)

**Eventos:**
- `@Output() onClick: EventEmitter<void>`

---

### Card

**Uso:**
```html
<app-card [elevation]="2" [hoverable]="true">
  <h3>Card Title</h3>
  <p>Card content goes here</p>
</app-card>
```

**Props:**
- `elevation`: 1 | 2 | 3 (padrão: 2)
- `flat`: boolean - Remove sombra, fundo cinza (padrão: false)
- `hoverable`: boolean - Adiciona hover effect (padrão: false)

---

### Alert

**Uso:**
```html
<app-alert type="success" [dismissible]="true">
  Operação concluída com sucesso!
</app-alert>
```

**Props:**
- `type`: 'info' | 'success' | 'warning' | 'error' (padrão: 'info')
- `dismissible`: boolean (padrão: false)

---

### Badge

**Uso:**
```html
<app-badge variant="success" size="lg">
  Ativo
</app-badge>

<app-badge variant="error" [outline]="true">
  Crítico
</app-badge>
```

**Props:**
- `variant`: 'default' | 'primary' | 'success' | 'warning' | 'error' | 'secondary'
- `size`: 'sm' | 'md' | 'lg' (padrão: 'md')
- `outline`: boolean (padrão: false)

---

## 🏗️ Componentes Base

### BaseSmartComponent

Para componentes que gerenciam estado e dados:

```typescript
import { Component, OnInit } from '@angular/core';
import { BaseSmartComponent } from '@shared/base-components';
import { takeUntil } from 'rxjs';

@Component({
  selector: 'app-my-smart',
  template: `...`
})
export class MySmartComponent extends BaseSmartComponent implements OnInit {
  loading = false;
  error: string | null = null;

  constructor(private api: ApiService) {
    super();
  }

  ngOnInit(): void {
    this.api.getData()
      .pipe(takeUntil(this.destroy$))  // Auto-unsubscribe on destroy
      .subscribe(...);
  }
}
```

**Funcionalidades:**
- `destroy$` - Subject para auto-limpeza de subscrições
- `loading` - Flag de carregamento
- `error` - Mensagem de erro
- `handleLoading<T>()` - Helper para gerenciar loading/erro
- `clearError()` - Limpar erro

---

### BasePresentationalComponent

Para componentes que apenas apresentam dados:

```typescript
import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { BasePresentationalComponent } from '@shared/base-components';

@Component({
  selector: 'app-my-dumb',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `...`
})
export class MyDumbComponent extends BasePresentationalComponent {
  @Input() data!: MyData;
  @Output() onAction = new EventEmitter<MyData>();
}
```

**Características:**
- Usa `ChangeDetectionStrategy.OnPush` (performance)
- Apenas `@Input/@Output`
- Sem lógica de negócio
- Sem chamadas à API
- Sem estado interno

---

## 📋 Boas Práticas

### ✅ DO's

1. **Use componentes base:**
   ```typescript
   export class MyComponent extends BaseSmartComponent { }
   ```

2. **Tipagem forte:**
   ```typescript
   @Input() items!: TodoModel[];
   @Output() onSelect = new EventEmitter<TodoModel>();
   ```

3. **OnPush para dumb components:**
   ```typescript
   @Component({ changeDetection: ChangeDetectionStrategy.OnPush })
   ```

4. **Auto-cleanup com destroy$:**
   ```typescript
   observable$.pipe(takeUntil(this.destroy$)).subscribe(...);
   ```

5. **Design tokens em estilos:**
   ```scss
   @import '@shared/design-tokens';
   color: $primary-600;
   padding: $spacing-4;
   ```

### ❌ DON'Ts

1. Não misture smart e dumb logic
2. Não faça chamadas à API em componentes dumb
3. Não mantenha estado mutável em dumb components
4. Não use ChangeDetectionStrategy.Default em dumb components
5. Não ignore tipagem TypeScript

---

## 🧪 Exemplos de Uso

### Usando Button com Loading

```typescript
export class MyComponent {
  isSubmitting = false;

  async handleSubmit() {
    this.isSubmitting = true;
    try {
      await this.api.save();
      this.showSuccess();
    } catch (err) {
      this.showError(err.message);
    } finally {
      this.isSubmitting = false;
    }
  }
}
```

```html
<app-button 
  [loading]="isSubmitting"
  (onClick)="handleSubmit()">
  Salvar
</app-button>
```

### Usando Badge com Status

```typescript
export class TaskListComponent {
  getStatusVariant(status: string): string {
    return status === 'done' ? 'success' : 'warning';
  }
}
```

```html
<app-badge [variant]="getStatusVariant(task.status)">
  {{ task.status }}
</app-badge>
```

### Usando Card em Grid

```scss
@import '@shared/design-tokens';

.card-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: $spacing-6;

  @include media-lg {
    grid-template-columns: repeat(3, 1fr);
  }
}
```

```html
<div class="card-grid">
  <app-card *ngFor="let item of items" [hoverable]="true">
    {{ item.name }}
  </app-card>
</div>
```

---

## 📚 Referências

- [Angular Component Architecture](https://angular.io/guide/component-interaction)
- [Smart vs Dumb Components](https://blog.thoughtram.io/components/2015/03/27/component-architecture-in-angular.html)
- [Design System Best Practices](https://www.designsystems.com/style-guides-vs-pattern-libraries/)
- [Design Tokens Methodology](https://github.com/design-tokens/community-group)

---

**Versão:** 1.0  
**Data:** 30 de Janeiro de 2026
