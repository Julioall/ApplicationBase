/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  darkMode: ['class', '[data-theme="dark"]'],
  theme: {
    extend: {
      // Tipografia
      fontFamily: {
        sans: ['-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'Roboto', '"Helvetica Neue"', 'Arial', 'sans-serif'],
        mono: ['"Courier New"', 'Courier', 'monospace'],
      },
      fontSize: {
        xs: ['0.75rem', { lineHeight: '1.25rem' }],     // 12px
        sm: ['0.875rem', { lineHeight: '1.5rem' }],     // 14px
        base: ['1rem', { lineHeight: '1.5rem' }],       // 16px
        lg: ['1.125rem', { lineHeight: '1.75rem' }],    // 18px
        xl: ['1.25rem', { lineHeight: '1.75rem' }],     // 20px
        '2xl': ['1.5rem', { lineHeight: '2rem' }],      // 24px
        '3xl': ['1.875rem', { lineHeight: '2.25rem' }], // 30px
        '4xl': ['2.25rem', { lineHeight: '2.5rem' }],   // 36px
        '5xl': ['3rem', { lineHeight: '3.5rem' }],      // 48px
      },
      fontWeight: {
        light: '300',
        normal: '400',
        medium: '500',
        semibold: '600',
        bold: '700',
        extrabold: '800',
      },

      // Cores - Design System
      colors: {
        // Backgrounds & Surfaces
        body: 'var(--bg-body)',
        page: 'var(--page-bg)',
        surface: 'var(--surface)',
        'surface-alt': 'var(--surface-alt)',
        'surface-muted': 'var(--surface-muted)',
        'surface-raised': 'var(--surface-raised)',
        'surface-strong': 'var(--surface-strong)',
        'surface-elevated': 'var(--surface-elevated)',

        // Borders
        'border-soft': 'var(--border-soft)',
        'border-strong': 'var(--border-strong)',

        // Text
        'text-primary': 'var(--text-primary)',
        'text-subtle': 'var(--text-subtle)',
        'text-muted': 'var(--text-muted)',
        'text-inverse': 'var(--text-inverse)',

        // Primary Colors (Azul)
        primary: 'var(--primary)',
        'primary-50': '#f0f9ff',
        'primary-100': '#e0f2fe',
        'primary-200': '#bae6fd',
        'primary-300': '#7dd3fc',
        'primary-400': '#38bdf8',
        'primary-500': '#0ea5e9',
        'primary-600': '#0284c7',
        'primary-700': '#0369a1',
        'primary-800': '#075985',
        'primary-900': '#0c3d66',
        'primary-strong': 'var(--primary-strong)',
        'primary-soft': 'var(--primary-soft)',

        // Secondary Colors (Roxo)
        secondary: {
          50: '#f5f3ff',
          100: '#ede9fe',
          200: '#ddd6fe',
          300: '#c4b5fd',
          400: '#a78bfa',
          500: '#8b5cf6',
          600: '#7c3aed',
          700: '#6d28d9',
          800: '#5b21b6',
          900: '#3f0f5c',
        },

        // Success (Verde)
        success: 'var(--success)',
        'success-50': '#f0fdf4',
        'success-100': '#dcfce7',
        'success-200': '#bbf7d0',
        'success-300': '#86efac',
        'success-400': '#4ade80',
        'success-500': '#22c55e',
        'success-600': '#16a34a',
        'success-700': '#15803d',
        'success-800': '#166534',
        'success-900': '#145231',

        // Warning (Laranja)
        warning: 'var(--warning)',
        'warning-50': '#fffbeb',
        'warning-100': '#fef3c7',
        'warning-200': '#fde68a',
        'warning-300': '#fcd34d',
        'warning-400': '#fbbf24',
        'warning-500': '#f59e0b',
        'warning-600': '#d97706',
        'warning-700': '#b45309',
        'warning-800': '#92400e',
        'warning-900': '#78350f',

        // Danger (Vermelho)
        danger: 'var(--danger)',
        'danger-50': '#fef2f2',
        'danger-100': '#fee2e2',
        'danger-200': '#fecaca',
        'danger-300': '#fca5a5',
        'danger-400': '#f87171',
        'danger-500': '#ef4444',
        'danger-600': '#dc2626',
        'danger-700': '#b91c1c',
        'danger-800': '#991b1b',
        'danger-900': '#7f1d1d',

        // Info (Ciano)
        info: 'var(--info)',
        'info-50': '#ecf0ff',
        'info-100': '#dde9ff',
        'info-200': '#cce0ff',
        'info-300': '#99bfff',
        'info-400': '#669eff',
        'info-500': '#337dff',
        'info-600': '#0052cc',
        'info-700': '#004399',
        'info-800': '#003366',
        'info-900': '#001a33',

        // Neutral (Cinza)
        gray: {
          50: '#f9fafb',
          100: '#f3f4f6',
          200: '#e5e7eb',
          300: '#d1d5db',
          400: '#9ca3af',
          500: '#6b7280',
          600: '#4b5563',
          700: '#374151',
          800: '#1f2937',
          900: '#111827',
        },

        // Overlays
        'overlay-strong': 'var(--overlay-strong)',
        'overlay-soft': 'var(--overlay-soft)',

        // Accent
        accent: 'var(--accent)',
        'accent-strong': 'var(--accent-strong)',
        'accent-soft': 'var(--accent-soft)',
      },

      // Espaçamento - Sistema 4px
      spacing: {
        0: '0',
        1: '0.25rem',    // 4px
        2: '0.5rem',     // 8px
        3: '0.75rem',    // 12px
        4: '1rem',       // 16px
        5: '1.25rem',    // 20px
        6: '1.5rem',     // 24px
        7: '1.75rem',    // 28px
        8: '2rem',       // 32px
        9: '2.25rem',    // 36px
        10: '2.5rem',    // 40px
        12: '3rem',      // 48px
        14: '3.5rem',    // 56px
        16: '4rem',      // 64px
        20: '5rem',      // 80px
        24: '6rem',      // 96px
        28: '7rem',      // 112px
        32: '8rem',      // 128px
        36: '9rem',      // 144px
        40: '10rem',     // 160px
        control: 'var(--control-height)',
      },

      // Sombras - Elevation Levels
      boxShadow: {
        // Elevation levels
        xs: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
        sm: '0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06)',
        md: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
        lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)',
        xl: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
        '2xl': '0 25px 50px -12px rgba(0, 0, 0, 0.25)',
        inner: 'inset 0 2px 4px 0 rgba(0, 0, 0, 0.05)',
        none: '0 0 0 rgba(0, 0, 0, 0)',

        // Aliases semânticas
        soft: 'var(--shadow-soft)',
        focus: '0 0 0 3px rgba(var(--focus-rgb), 0.35)',

        // Hover states
        'hover-sm': '0 1px 3px 0 rgba(0, 0, 0, 0.15)',
        'hover-md': '0 4px 6px -1px rgba(0, 0, 0, 0.15)',
        'hover-lg': '0 10px 15px -3px rgba(0, 0, 0, 0.15)',
      },

      // Border Radius
      borderRadius: {
        none: '0',
        sm: '0.125rem',   // 2px
        md: 'var(--radius-md)',
        lg: 'var(--radius-lg)',
        xl: 'var(--radius-xl)',
        '2xl': '1rem',    // 16px
        '3xl': '1.5rem',  // 24px
        full: '9999px',
      },

      // Transições
      transitionDuration: {
        fast: '150ms',
        base: '250ms',
        slow: '350ms',
        slower: '500ms',
      },
      transitionTimingFunction: {
        'in-out': 'cubic-bezier(0.4, 0, 0.2, 1)',
      },

      // Z-index
      zIndex: {
        0: '0',
        10: '10',
        20: '20',
        30: '30',
        40: '40',
        50: '50',
        dropdown: '1000',
        sticky: '1020',
        fixed: '1030',
        'modal-backdrop': '1040',
        modal: '1050',
        popover: '1060',
        tooltip: '1070',
      },

      // Ring colors
      ringColor: {
        focus: 'rgba(var(--focus-rgb), 0.35)',
      },
      ringOffsetColor: {
        surface: 'var(--surface)',
        'surface-alt': 'var(--surface-alt)',
      },
    },
  },
  plugins: [],
}
