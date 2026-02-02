/**
 * UI COMPONENTS EXPORTS
 * Exportação centralizada de todos os componentes do design system
 */

// Componentes Base
export { ButtonComponent } from './button/button.component';
export { InputComponent } from './input/input.component';
export { CardComponent } from './card/card.component';
export { LoadingComponent, type LoadingType } from './loading/loading.component';

// Services e Tokens
export { DesignSystemService, type Theme, type ComponentSize, type ComponentVariant } from '../../services/design-system.service';
export { designTokens, DesignSystem, componentStyles } from '../../design-tokens';

// Array de todos os componentes para importação fácil
export const UI_COMPONENTS = [
  ButtonComponent,
  InputComponent,
  CardComponent,
  LoadingComponent,
] as const;