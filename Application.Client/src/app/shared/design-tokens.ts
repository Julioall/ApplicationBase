/**
 * DESIGN TOKENS
 * Tokens de design exportados para uso em TypeScript/Angular
 * Mantém consistência entre CSS e componentes JavaScript
 */

export interface ColorPalette {
  50: string;
  100: string;
  200: string;
  300: string;
  400: string;
  500: string;
  600: string;
  700: string;
  800: string;
  900: string;
  950: string;
}

export const designTokens = {
  // CORES PRIMÁRIAS
  colors: {
    primary: {
      50: '#f0f9ff',
      100: '#e0f2fe',
      200: '#bae6fd',
      300: '#7dd3fc',
      400: '#38bdf8',
      500: '#0ea5e9',
      600: '#0284c7',
      700: '#0369a1',
      800: '#075985',
      900: '#0c4a6e',
      950: '#082f49',
    } as ColorPalette,
    
    secondary: {
      50: '#f8fafc',
      100: '#f1f5f9',
      200: '#e2e8f0',
      300: '#cbd5e1',
      400: '#94a3b8',
      500: '#64748b',
      600: '#475569',
      700: '#334155',
      800: '#1e293b',
      900: '#0f172a',
      950: '#020617',
    } as ColorPalette,
    
    accent: {
      50: '#fdf4ff',
      100: '#fae8ff',
      200: '#f5d0fe',
      300: '#f0abfc',
      400: '#e879f9',
      500: '#d946ef',
      600: '#c026d3',
      700: '#a21caf',
      800: '#86198f',
      900: '#701a75',
      950: '#4a044e',
    } as ColorPalette,
    
    success: {
      50: '#f0fdf4',
      100: '#dcfce7',
      200: '#bbf7d0',
      300: '#86efac',
      400: '#4ade80',
      500: '#22c55e',
      600: '#16a34a',
      700: '#15803d',
      800: '#166534',
      900: '#14532d',
      950: '#052e16',
    } as ColorPalette,
    
    warning: {
      50: '#fffbeb',
      100: '#fef3c7',
      200: '#fde68a',
      300: '#fcd34d',
      400: '#fbbf24',
      500: '#f59e0b',
      600: '#d97706',
      700: '#b45309',
      800: '#92400e',
      900: '#78350f',
      950: '#451a03',
    } as ColorPalette,
    
    error: {
      50: '#fef2f2',
      100: '#fee2e2',
      200: '#fecaca',
      300: '#fca5a5',
      400: '#f87171',
      500: '#ef4444',
      600: '#dc2626',
      700: '#b91c1c',
      800: '#991b1b',
      900: '#7f1d1d',
      950: '#450a0a',
    } as ColorPalette,
    
    neutral: {
      50: '#fafafa',
      100: '#f5f5f5',
      200: '#e5e5e5',
      300: '#d4d4d4',
      400: '#a3a3a3',
      500: '#737373',
      600: '#525252',
      700: '#404040',
      800: '#262626',
      900: '#171717',
      950: '#0a0a0a',
    } as ColorPalette,
  },

  // TIPOGRAFIA
  typography: {
    fontFamily: {
      sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
      mono: ['JetBrains Mono', 'ui-monospace', 'SFMono-Regular', 'monospace'],
      heading: ['Poppins', 'ui-sans-serif', 'system-ui', 'sans-serif'],
    },
    fontSize: {
      xs: ['0.75rem', { lineHeight: '1rem', letterSpacing: '0.025em' }],
      sm: ['0.875rem', { lineHeight: '1.25rem', letterSpacing: '0.025em' }],
      base: ['1rem', { lineHeight: '1.5rem', letterSpacing: '0em' }],
      lg: ['1.125rem', { lineHeight: '1.75rem', letterSpacing: '-0.025em' }],
      xl: ['1.25rem', { lineHeight: '1.75rem', letterSpacing: '-0.025em' }],
      '2xl': ['1.5rem', { lineHeight: '2rem', letterSpacing: '-0.025em' }],
      '3xl': ['1.875rem', { lineHeight: '2.25rem', letterSpacing: '-0.025em' }],
      '4xl': ['2.25rem', { lineHeight: '2.5rem', letterSpacing: '-0.05em' }],
      '5xl': ['3rem', { lineHeight: '1', letterSpacing: '-0.05em' }],
    },
    fontWeight: {
      thin: '100',
      extralight: '200',
      light: '300',
      normal: '400',
      medium: '500',
      semibold: '600',
      bold: '700',
      extrabold: '800',
      black: '900',
    },
  },

  // ESPAÇAMENTOS
  spacing: {
    px: '1px',
    0: '0px',
    0.5: '0.125rem',
    1: '0.25rem',
    1.5: '0.375rem',
    2: '0.5rem',
    2.5: '0.625rem',
    3: '0.75rem',
    3.5: '0.875rem',
    4: '1rem',
    5: '1.25rem',
    6: '1.5rem',
    7: '1.75rem',
    8: '2rem',
    9: '2.25rem',
    10: '2.5rem',
    11: '2.75rem',
    12: '3rem',
    14: '3.5rem',
    16: '4rem',
    18: '4.5rem',
    20: '5rem',
    24: '6rem',
    28: '7rem',
    32: '8rem',
    36: '9rem',
    40: '10rem',
    44: '11rem',
    48: '12rem',
    52: '13rem',
    56: '14rem',
    60: '15rem',
    64: '16rem',
    72: '18rem',
    80: '20rem',
    88: '22rem',
    96: '24rem',
    100: '25rem',
    112: '28rem',
    128: '32rem',
  },

  // SOMBRAS
  shadows: {
    xs: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
    sm: '0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06)',
    md: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
    lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)',
    xl: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
    '2xl': '0 25px 50px -12px rgba(0, 0, 0, 0.25)',
    soft: '0 2px 15px 0 rgba(0, 0, 0, 0.08)',
    medium: '0 4px 20px 0 rgba(0, 0, 0, 0.12)',
    hard: '0 8px 30px 0 rgba(0, 0, 0, 0.16)',
    glass: '0 8px 32px 0 rgba(31, 38, 135, 0.37)',
    glow: {
      primary: '0 0 20px rgba(14, 165, 233, 0.5)',
      accent: '0 0 20px rgba(217, 70, 239, 0.5)',
      success: '0 0 20px rgba(34, 197, 94, 0.5)',
      error: '0 0 20px rgba(239, 68, 68, 0.5)',
    },
  },

  // BORDER RADIUS
  borderRadius: {
    none: '0',
    sm: '0.125rem',
    default: '0.375rem',
    md: '0.5rem',
    lg: '0.75rem',
    xl: '1rem',
    '2xl': '1.5rem',
    '3xl': '2rem',
    full: '9999px',
  },

  // Z-INDEX
  zIndex: {
    auto: 'auto',
    0: 0,
    10: 10,
    20: 20,
    30: 30,
    40: 40,
    50: 50,
    overlay: 1000,
    dropdown: 1010,
    modal: 1020,
    popover: 1030,
    tooltip: 1040,
    toast: 1050,
  },

  // ANIMAÇÕES
  animations: {
    duration: {
      75: '75ms',
      100: '100ms',
      150: '150ms',
      200: '200ms',
      300: '300ms',
      500: '500ms',
      700: '700ms',
      1000: '1000ms',
    },
    easing: {
      linear: 'linear',
      in: 'cubic-bezier(0.4, 0, 1, 1)',
      out: 'cubic-bezier(0, 0, 0.2, 1)',
      'in-out': 'cubic-bezier(0.4, 0, 0.2, 1)',
      'ease-in-quart': 'cubic-bezier(0.5, 0, 0.75, 0)',
      'ease-out-quart': 'cubic-bezier(0.25, 1, 0.5, 1)',
      'ease-in-out-quart': 'cubic-bezier(0.76, 0, 0.24, 1)',
      'ease-in-expo': 'cubic-bezier(0.7, 0, 0.84, 0)',
      'ease-out-expo': 'cubic-bezier(0.16, 1, 0.3, 1)',
      'ease-in-out-expo': 'cubic-bezier(0.87, 0, 0.13, 1)',
    },
  },

  // BREAKPOINTS
  breakpoints: {
    xs: '475px',
    sm: '640px',
    md: '768px',
    lg: '1024px',
    xl: '1280px',
    '2xl': '1536px',
  },
} as const;

// UTILITÁRIOS PARA USAGE EM ANGULAR
export class DesignSystem {
  static getColor(colorName: keyof typeof designTokens.colors, shade: keyof ColorPalette = '500'): string {
    return designTokens.colors[colorName][shade];
  }

  static getShadow(shadowName: keyof typeof designTokens.shadows): string {
    const shadow = designTokens.shadows[shadowName];
    return typeof shadow === 'string' ? shadow : '';
  }

  static getSpacing(spacingName: keyof typeof designTokens.spacing): string {
    return designTokens.spacing[spacingName];
  }

  static getFontSize(sizeName: keyof typeof designTokens.typography.fontSize): string {
    const fontSize = designTokens.typography.fontSize[sizeName];
    return Array.isArray(fontSize) ? fontSize[0] : fontSize;
  }

  static getBorderRadius(radiusName: keyof typeof designTokens.borderRadius): string {
    return designTokens.borderRadius[radiusName];
  }

  static getBreakpoint(breakpointName: keyof typeof designTokens.breakpoints): string {
    return designTokens.breakpoints[breakpointName];
  }

  static getAnimationDuration(durationName: keyof typeof designTokens.animations.duration): string {
    return designTokens.animations.duration[durationName];
  }

  static getAnimationEasing(easingName: keyof typeof designTokens.animations.easing): string {
    return designTokens.animations.easing[easingName];
  }

  // Helpers para temas
  static createColorVariant(baseColor: string, opacity: number = 1): string {
    // Converte hex para rgba
    const hex = baseColor.replace('#', '');
    const r = parseInt(hex.substr(0, 2), 16);
    const g = parseInt(hex.substr(2, 2), 16);
    const b = parseInt(hex.substr(4, 2), 16);
    
    return `rgba(${r}, ${g}, ${b}, ${opacity})`;
  }

  static createGradient(startColor: string, endColor: string, direction: string = '135deg'): string {
    return `linear-gradient(${direction}, ${startColor} 0%, ${endColor} 100%)`;
  }

  // Media queries helpers
  static mediaQuery(breakpoint: keyof typeof designTokens.breakpoints): string {
    return `(min-width: ${designTokens.breakpoints[breakpoint]})`;
  }

  static mediaQueryMax(breakpoint: keyof typeof designTokens.breakpoints): string {
    const value = designTokens.breakpoints[breakpoint];
    const numValue = parseInt(value) - 1;
    const unit = value.replace(/\d+/g, '');
    return `(max-width: ${numValue}${unit})`;
  }
}

// CONSTANTES DE COMPONENTES
export const componentStyles = {
  // Botões
  button: {
    base: 'inline-flex items-center justify-center font-medium rounded-lg transition-all duration-200 ease-out focus:outline-none focus:ring-4 disabled:opacity-50 disabled:cursor-not-allowed',
    sizes: {
      xs: 'px-3 py-1.5 text-xs',
      sm: 'px-4 py-2 text-sm',
      md: 'px-6 py-3 text-base',
      lg: 'px-8 py-4 text-lg',
      xl: 'px-10 py-5 text-xl',
    },
    variants: {
      primary: 'text-white bg-gradient-primary hover:from-primary-600 hover:to-primary-700 focus:ring-primary-200',
      secondary: 'text-gray-700 bg-white border-2 border-gray-300 hover:bg-gray-50 hover:border-gray-400 focus:ring-gray-200',
      ghost: 'text-gray-600 bg-transparent hover:bg-gray-100 focus:ring-gray-200',
      danger: 'text-white bg-gradient-to-r from-error-500 to-error-600 hover:from-error-600 hover:to-error-700 focus:ring-error-200',
    },
  },

  // Inputs
  input: {
    base: 'w-full border-2 rounded-lg transition-all duration-200 ease-out focus:outline-none focus:ring-4 placeholder:text-gray-400',
    sizes: {
      sm: 'px-3 py-2 text-sm',
      md: 'px-4 py-3 text-base',
      lg: 'px-5 py-4 text-lg',
    },
    variants: {
      default: 'border-gray-200 focus:border-primary-500 focus:ring-primary-100',
      error: 'border-error-500 focus:border-error-500 focus:ring-error-100',
      success: 'border-success-500 focus:border-success-500 focus:ring-success-100',
    },
  },

  // Cards
  card: {
    base: 'rounded-lg transition-all duration-200',
    variants: {
      elevated: 'bg-white shadow-lg border border-gray-100 hover:shadow-xl',
      flat: 'bg-white border border-gray-200 hover:border-gray-300',
      glass: 'glass-container',
    },
    padding: {
      none: '',
      sm: 'p-4',
      md: 'p-6',
      lg: 'p-8',
      xl: 'p-10',
    },
  },
} as const;

export default designTokens;