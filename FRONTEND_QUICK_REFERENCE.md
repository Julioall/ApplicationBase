# 🎯 Guia Rápido - Frontend Angular 18 Organizado

## 📍 Encontre rapidamente o que precisa

### 🏗️ Arquitetura

**Estrutura de Pastas:**
- `core/` → Serviços globais, interceptors, guards
- `features/` → Módulos (todo, auth, etc)
- `shared/` → Componentes, tokens, utilitários
- `layouts/` → Layouts de página

**Separação de Responsabilidades:**
- `containers/` → Smart components (estado, API)
- `components/` → Dumb components (apenas apresentação)
- `services/` → Façade services (orquestração)

---

## 🎨 Design System

### Usar Cores
```scss
@import '@shared/design-tokens';

// Cores
color: $primary-600;      // Azul principal
color: $success-600;      // Verde
color: $error-600;        // Vermelho
color: $gray-900;         // Texto escuro
```

### Usar Espaçamento
```scss
padding: $spacing-4;      // 16px
margin: $spacing-6;       // 24px
gap: $spacing-2;          // 8px
```

### Usar Sombras
```scss
@include elevation(2);    // Sombra média
@include elevation(4);    // Sombra grande
```

### Responsive
```scss
@include media-lg {
  // CSS que aplica em screens >= 1024px
}
```

### Tipografia
```scss
@include h1-style;        // Heading 1
@include body-style;      // Body text
@include caption-style;   // Caption
```

---

## 🧩 Componentes Reutilizáveis

### Button
```html
<app-button 
  variant="primary"
  (buttonClick)="save()">
  Salvar
</app-button>
```
Props: `variant`, `size`, `loading`, `disabled`

### Card
```html
<app-card [elevation]="2" [hoverable]="true">
  Content here
</app-card>
```
Props: `elevation`, `flat`, `hoverable`

### Alert
```html
<app-alert type="success" [dismissible]="true">
  Sucesso!
</app-alert>
```
Props: `type`, `dismissible`

### Badge
```html
<app-badge variant="success">
  Ativo
</app-badge>
```
Props: `variant`, `size`, `outline`

---

## 🏗️ Criar Componentes

### Smart Component (Container)
```typescript
import { Component, OnInit } from '@angular/core';
import { BaseSmartComponent } from '@shared/base-components';

@Component({
  selector: 'app-my-container',
  template: `
    <div *ngIf="loading$ | async">Loading...</div>
    <div *ngFor="let item of (items$ | async) as items">
      {{ item.name }}
    </div>
  `
})
export class MyContainerComponent extends BaseSmartComponent implements OnInit {
  items$ = this.facade.items;
  
  constructor(private facade: MyFacadeService) { super(); }
  
  ngOnInit(): void {
    this.facade.load();
  }
}
```

### Dumb Component (Presentational)
```typescript
import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { BasePresentationalComponent } from '@shared/base-components';

@Component({
  selector: 'app-my-item',
  template: `
    <div>{{ item.name }}</div>
    <button (click)="select.emit(item)">Select</button>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyItemComponent extends BasePresentationalComponent {
  @Input() item!: MyModel;
  @Output() select = new EventEmitter<MyModel>();
}
```

### Façade Service
```typescript
import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class MyFacadeService {
  items$ = new BehaviorSubject<MyModel[]>([]);
  loading$ = new BehaviorSubject(false);
  
  load(): void {
    this.loading$.next(true);
    // Lógica de carregamento
  }
}
```

---

## 📦 Imports Comuns

### Componentes Base
```typescript
import { BaseSmartComponent } from '@shared/base-components';
import { BasePresentationalComponent } from '@shared/base-components';
```

### UI Components
```typescript
import { ButtonComponent, CardComponent, AlertComponent, BadgeComponent } from '@shared/components';
```

### RxJS Operators
```typescript
import { takeUntil, map, filter } from 'rxjs';

// No smart component:
observable$.pipe(
  takeUntil(this.destroy$),
  map(data => data.value),
  filter(value => value > 0)
).subscribe(...);
```

### Angular Common
```typescript
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
```

---

## 🧪 Testes

### Teste Componentado
```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MyComponent } from './my.component';

describe('MyComponent', () => {
  let component: MyComponent;
  let fixture: ComponentFixture<MyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MyComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(MyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar', () => {
    expect(component).toBeTruthy();
  });

  it('deve emitir evento', () => {
    spyOn(component.myOutput, 'emit');
    component.handleClick();
    expect(component.myOutput.emit).toHaveBeenCalled();
  });
});
```

### Teste E2E (Playwright)
```typescript
import { test, expect } from '@playwright/test';

test('deve exibir página', async ({ page }) => {
  await page.goto('http://localhost:4200/my-page');
  const title = page.locator('h1');
  await expect(title).toContainText('My Title');
});
```

---

## 🔍 Debugging

### Verificar Dados
```html
<!-- Debug template -->
<pre>{{ myData | json }}</pre>
<pre>{{ myObservable$ | async | json }}</pre>
```

### Console Log em Service
```typescript
constructor(private http: HttpClient) {
  console.log('Service initialized');
}

load(): void {
  console.time('load');
  this.http.get('...').subscribe(data => {
    console.log('Data loaded:', data);
    console.timeEnd('load');
  });
}
```

### DevTools
```
F12 → Elements      (inspecionar HTML)
F12 → Console       (ver logs)
F12 → Network       (ver requisições)
F12 → Performance   (medir performance)
```

---

## ✅ Checklist Antes de Commitar

- [ ] ESLint passou: `npm run lint`
- [ ] Testes passaram: `npm test`
- [ ] Sem console.log em produção
- [ ] Tipagem TypeScript completa
- [ ] Componentes estão simples (< 300 linhas)
- [ ] README atualizado
- [ ] Imports organizados (padrão Angular)
- [ ] Seguiu naming conventions

---

## 📝 Naming Conventions

| O quê | Padrão | Exemplo |
|-------|--------|---------|
| Componentes | PascalCase | `MyComponent` |
| Arquivos | kebab-case | `my-component.ts` |
| Propriedades | camelCase | `myProperty` |
| Constantes | UPPER_SNAKE_CASE | `MY_CONSTANT` |
| Outputs | Sem "on" | `select`, `edit` (não onSelect) |
| Directives | camelCase | `appHighlight` |
| Pipes | camelCase | `appCurrency` |

---

## 🎓 Aprendizados Principais

1. **Smart vs Dumb**
   - Smart: Orquestra, fetch, estado
   - Dumb: Apresenta, reusável

2. **Change Detection**
   - OnPush em dumb components = performance
   - Uso de observables com | async

3. **RxJS Lifecycle**
   - takeUntil(this.destroy$) evita memory leaks
   - BehaviorSubject para estado compartilhado

4. **Design System**
   - Tokens centralizados
   - Reutilização máxima
   - Escalável

5. **Testes**
   - Unit tests para lógica
   - E2E tests para fluxos
   - Mínimo 80% cobertura

---

## 🚀 Próximos Passos

1. **Integração API**
   - Substituir mocks por HTTP real
   - Implementar erro handling

2. **Componentes Avançados**
   - Forms, inputs, selects
   - Data tables, paginação
   - Modais, sidebars

3. **Performance**
   - Lazy loading
   - Virtual scrolling
   - Code splitting

4. **UX**
   - Dark mode
   - Animações
   - Loading states

5. **Acessibilidade**
   - WCAG 2.1 AA
   - Keyboard navigation
   - Screen reader support

---

## 📚 Referências Rápidas

- **Angular Docs:** https://angular.io
- **RxJS Docs:** https://rxjs.dev
- **Tailwind CSS:** https://tailwindcss.com
- **Design Tokens:** https://github.com/design-tokens
- **Playwright:** https://playwright.dev

---

**Dúvidas?** Consulte:
1. `docs/features/frontend-organization.md`
2. `src/app/shared/README.md`
3. Código existente nos componentes

**Versão:** 1.0 | **Data:** 30/01/2026 | ✅ Pronto
