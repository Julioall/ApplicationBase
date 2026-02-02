/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  darkMode: 'class',
  theme: {
    extend: {
      // DESIGN TOKENS - CORES REFINADAS
      colors: {
        primary: {
          50: '#f0f9ff',
          100: '#e0f2fe', 
          200: '#bae6fd',
          300: '#7dd3fc',
          400: '#38bdf8',
          500: '#0ea5e9', // Primary
          600: '#0284c7',
          700: '#0369a1',
          800: '#075985',
          900: '#0c4a6e',
          950: '#082f49',
        },
        secondary: {
          50: '#f8fafc',
          100: '#f1f5f9',
          200: '#e2e8f0',
          300: '#cbd5e1',
          400: '#94a3b8',
          500: '#64748b', // Secondary
          600: '#475569',
          700: '#334155',
          800: '#1e293b',
          900: '#0f172a',
          950: '#020617',
        },
        accent: {
          50: '#fdf4ff',
          100: '#fae8ff',
          200: '#f5d0fe',
          300: '#f0abfc',
          400: '#e879f9',
          500: '#d946ef', // Accent
          600: '#c026d3',
          700: '#a21caf',
          800: '#86198f',
          900: '#701a75',
          950: '#4a044e',
        },
        success: {
          50: '#f0fdf4',
          100: '#dcfce7',
          200: '#bbf7d0',
          300: '#86efac',
          400: '#4ade80',
          500: '#22c55e', // Success
          600: '#16a34a',
          700: '#15803d',
          800: '#166534',
          900: '#14532d',
          950: '#052e16',
        },
        warning: {
          50: '#fffbeb',
          100: '#fef3c7',
          200: '#fde68a',
          300: '#fcd34d',
          400: '#fbbf24',
          500: '#f59e0b', // Warning
          600: '#d97706',
          700: '#b45309',
          800: '#92400e',
          900: '#78350f',
          950: '#451a03',
        },
        error: {
          50: '#fef2f2',
          100: '#fee2e2',
          200: '#fecaca',
          300: '#fca5a5',
          400: '#f87171',
          500: '#ef4444', // Error
          600: '#dc2626',
          700: '#b91c1c',
          800: '#991b1b',
          900: '#7f1d1d',
          950: '#450a0a',
        },
        // Cores neutras estendidas
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
        },
        // Gradientes personalizados
        'glass-light': 'rgba(255, 255, 255, 0.25)',
        'glass-dark': 'rgba(17, 25, 40, 0.25)',
        'backdrop-light': 'rgba(255, 255, 255, 0.1)',
        'backdrop-dark': 'rgba(17, 25, 40, 0.1)',
      },

      // GRADIENTES MODERNOS
      backgroundImage: {
        'gradient-radial': 'radial-gradient(var(--tw-gradient-stops))',
        'gradient-conic': 'conic-gradient(from 180deg at 50% 50%, var(--tw-gradient-stops))',
        'gradient-primary': 'linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%)',
        'gradient-secondary': 'linear-gradient(135deg, #64748b 0%, #475569 100%)',
        'gradient-accent': 'linear-gradient(135deg, #d946ef 0%, #c026d3 100%)',
        'gradient-success': 'linear-gradient(135deg, #22c55e 0%, #16a34a 100%)',
        'gradient-glass-light': 'linear-gradient(135deg, rgba(255,255,255,0.1) 0%, rgba(255,255,255,0.2) 100%)',
        'gradient-glass-dark': 'linear-gradient(135deg, rgba(17,25,40,0.1) 0%, rgba(17,25,40,0.2) 100%)',
        'mesh-gradient': 'radial-gradient(at 40% 20%, #0ea5e9 0%, transparent 50%), radial-gradient(at 80% 0%, #d946ef 0%, transparent 50%), radial-gradient(at 0% 50%, #22c55e 0%, transparent 50%), radial-gradient(at 80% 50%, #f59e0b 0%, transparent 50%), radial-gradient(at 0% 100%, #ef4444 0%, transparent 50%), radial-gradient(at 80% 100%, #0ea5e9 0%, transparent 50%), radial-gradient(at 0% 0%, #d946ef 0%, transparent 50%)',
      },

      // TIPOGRAFIA ESCALONADA
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'ui-monospace', 'SFMono-Regular', 'monospace'],
        heading: ['Poppins', 'ui-sans-serif', 'system-ui', 'sans-serif'],
      },
      fontSize: {
        'xs': ['0.75rem', { lineHeight: '1rem', letterSpacing: '0.025em' }],
        'sm': ['0.875rem', { lineHeight: '1.25rem', letterSpacing: '0.025em' }],
        'base': ['1rem', { lineHeight: '1.5rem', letterSpacing: '0em' }],
        'lg': ['1.125rem', { lineHeight: '1.75rem', letterSpacing: '-0.025em' }],
        'xl': ['1.25rem', { lineHeight: '1.75rem', letterSpacing: '-0.025em' }],
        '2xl': ['1.5rem', { lineHeight: '2rem', letterSpacing: '-0.025em' }],
        '3xl': ['1.875rem', { lineHeight: '2.25rem', letterSpacing: '-0.025em' }],
        '4xl': ['2.25rem', { lineHeight: '2.5rem', letterSpacing: '-0.05em' }],
        '5xl': ['3rem', { lineHeight: '1', letterSpacing: '-0.05em' }],
        '6xl': ['3.75rem', { lineHeight: '1', letterSpacing: '-0.05em' }],
        '7xl': ['4.5rem', { lineHeight: '1', letterSpacing: '-0.075em' }],
        '8xl': ['6rem', { lineHeight: '1', letterSpacing: '-0.075em' }],
        '9xl': ['8rem', { lineHeight: '1', letterSpacing: '-0.075em' }],
      },

      // ESPAÇAMENTO HARMÔNICO
      spacing: {
        '18': '4.5rem',
        '88': '22rem',
        '100': '25rem',
        '112': '28rem',
        '128': '32rem',
      },

      // BORDER RADIUS MODERNO
      borderRadius: {
        'none': '0',
        'sm': '0.125rem',
        'DEFAULT': '0.375rem',
        'md': '0.5rem',
        'lg': '0.75rem',
        'xl': '1rem',
        '2xl': '1.5rem',
        '3xl': '2rem',
        'full': '9999px',
      },

      // SOMBRAS SOFISTICADAS
      boxShadow: {
        'xs': '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
        'sm': '0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06)',
        'md': '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
        'lg': '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)',
        'xl': '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
        '2xl': '0 25px 50px -12px rgba(0, 0, 0, 0.25)',
        'inner': 'inset 0 2px 4px 0 rgba(0, 0, 0, 0.06)',
        'none': 'none',
        // Sombras customizadas
        'soft': '0 2px 15px 0 rgba(0, 0, 0, 0.08)',
        'medium': '0 4px 20px 0 rgba(0, 0, 0, 0.12)',
        'hard': '0 8px 30px 0 rgba(0, 0, 0, 0.16)',
        'brutal': '0 12px 40px 0 rgba(0, 0, 0, 0.24)',
        'glass': '0 8px 32px 0 rgba(31, 38, 135, 0.37)',
        'glow-primary': '0 0 20px rgba(14, 165, 233, 0.5)',
        'glow-accent': '0 0 20px rgba(217, 70, 239, 0.5)',
        'glow-success': '0 0 20px rgba(34, 197, 94, 0.5)',
        'glow-error': '0 0 20px rgba(239, 68, 68, 0.5)',
      },

      // BACKDROP FILTERS
      backdropBlur: {
        xs: '2px',
        sm: '4px',
        md: '8px',
        lg: '12px',
        xl: '16px',
        '2xl': '24px',
        '3xl': '40px',
      },

      // ANIMAÇÕES FLUIDAS
      animation: {
        'fade-in': 'fadeIn 0.5s ease-in-out',
        'fade-in-up': 'fadeInUp 0.6s ease-out',
        'fade-in-down': 'fadeInDown 0.6s ease-out',
        // 'slide-in-right': 'slideInRight 0.5s ease-out', // Removida - movimento indesejado
        'slide-in-left': 'slideInLeft 0.5s ease-out',
        'bounce-soft': 'bounceSoft 2s infinite',
        'pulse-soft': 'pulseSoft 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'float': 'float 3s ease-in-out infinite',
        'shake': 'shake 0.82s cubic-bezier(.36,.07,.19,.97) both',
        'glow': 'glow 2s ease-in-out infinite alternate',
        'ripple': 'ripple 0.6s ease-out',
        'scale-in': 'scaleIn 0.2s ease-out',
        'scale-out': 'scaleOut 0.2s ease-in',
      },

      // KEYFRAMES PERSONALIZADOS
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        fadeInUp: {
          '0%': { opacity: '0', transform: 'translateY(30px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        fadeInDown: {
          '0%': { opacity: '0', transform: 'translateY(-30px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        // slideInRight: {
        //   '0%': { opacity: '0', transform: 'translateX(100px)' },
        //   '100%': { opacity: '1', transform: 'translateX(0)' },
        // }, // Removida - movimento indesejado da direita para esquerda
        slideInLeft: {
          '0%': { opacity: '0', transform: 'translateX(-100px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
        bounceSoft: {
          '0%, 20%, 53%, 80%, 100%': { transform: 'translate3d(0,0,0)' },
          '40%, 43%': { transform: 'translate3d(0, -30px, 0)' },
          '70%': { transform: 'translate3d(0, -15px, 0)' },
          '90%': { transform: 'translate3d(0, -4px, 0)' },
        },
        pulseSoft: {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.8' },
        },
        float: {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-20px)' },
        },
        shake: {
          '10%, 90%': { transform: 'translate3d(-1px, 0, 0)' },
          '20%, 80%': { transform: 'translate3d(2px, 0, 0)' },
          '30%, 50%, 70%': { transform: 'translate3d(-4px, 0, 0)' },
          '40%, 60%': { transform: 'translate3d(4px, 0, 0)' },
        },
        glow: {
          '0%': { boxShadow: '0 0 5px rgba(14, 165, 233, 0.5)' },
          '100%': { boxShadow: '0 0 20px rgba(14, 165, 233, 0.8)' },
        },
        ripple: {
          '0%': { transform: 'scale(0)', opacity: '0.5' },
          '100%': { transform: 'scale(4)', opacity: '0' },
        },
        scaleIn: {
          '0%': { transform: 'scale(0.9)', opacity: '0' },
          '100%': { transform: 'scale(1)', opacity: '1' },
        },
        scaleOut: {
          '0%': { transform: 'scale(1)', opacity: '1' },
          '100%': { transform: 'scale(0.9)', opacity: '0' },
        },
      },

      // DURAÇÕES E EASING
      transitionDuration: {
        '0': '0ms',
        '75': '75ms',
        '100': '100ms',
        '150': '150ms',
        '200': '200ms',
        '300': '300ms',
        '400': '400ms',
        '500': '500ms',
        '700': '700ms',
        '1000': '1000ms',
      },
      
      transitionTimingFunction: {
        'ease-in-quart': 'cubic-bezier(0.5, 0, 0.75, 0)',
        'ease-out-quart': 'cubic-bezier(0.25, 1, 0.5, 1)',
        'ease-in-out-quart': 'cubic-bezier(0.76, 0, 0.24, 1)',
        'ease-in-expo': 'cubic-bezier(0.7, 0, 0.84, 0)',
        'ease-out-expo': 'cubic-bezier(0.16, 1, 0.3, 1)',
        'ease-in-out-expo': 'cubic-bezier(0.87, 0, 0.13, 1)',
      },

      // Z-INDEX ESCALONADO
      zIndex: {
        '-1': '-1',
        '0': '0',
        '10': '10',
        '20': '20',
        '30': '30',
        '40': '40',
        '50': '50',
        '60': '60',
        '70': '70',
        '80': '80',
        '90': '90',
        '100': '100',
        'overlay': '1000',
        'dropdown': '1010',
        'modal': '1020',
        'popover': '1030',
        'tooltip': '1040',
        'toast': '1050',
      },
    },
  },
  plugins: [
    // Plugin para componentes customizados
    function({ addComponents, theme }) {
      addComponents({
        // GLASS MORPHISM CARDS
        '.glass-card': {
          background: 'rgba(255, 255, 255, 0.25)',
          backdropFilter: 'blur(10px)',
          border: '1px solid rgba(255, 255, 255, 0.18)',
          boxShadow: '0 8px 32px 0 rgba(31, 38, 135, 0.37)',
        },
        '.glass-card-dark': {
          background: 'rgba(17, 25, 40, 0.25)',
          backdropFilter: 'blur(10px)',
          border: '1px solid rgba(255, 255, 255, 0.125)',
          boxShadow: '0 8px 32px 0 rgba(31, 38, 135, 0.37)',
        },

        // INPUT COMPONENTS
        '.input-modern': {
          '@apply w-full px-4 py-3 text-base border-2 border-gray-200 rounded-lg': {},
          '@apply focus:border-primary-500 focus:ring-4 focus:ring-primary-100': {},
          '@apply transition-all duration-200 ease-out': {},
          '@apply placeholder:text-gray-400': {},
          '&:focus': {
            '@apply outline-none border-primary-500 ring-4 ring-primary-100': {},
          },
          '&.error': {
            '@apply border-error-500 focus:border-error-500 focus:ring-error-100': {},
          },
          '&.success': {
            '@apply border-success-500 focus:border-success-500 focus:ring-success-100': {},
          },
        },
        '.input-modern-dark': {
          '@apply bg-gray-800 border-gray-700 text-white placeholder:text-gray-500': {},
          '@apply focus:border-primary-400 focus:ring-primary-900': {},
        },

        // BUTTON COMPONENTS
        '.btn-primary': {
          '@apply inline-flex items-center justify-center px-6 py-3 text-base font-medium': {},
          '@apply text-white bg-gradient-primary rounded-lg': {},
          '@apply hover:from-primary-600 hover:to-primary-700': {},
          '@apply focus:outline-none focus:ring-4 focus:ring-primary-200': {},
          '@apply transform hover:scale-105 active:scale-95': {},
          '@apply transition-all duration-200 ease-out': {},
          '@apply disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none': {},
        },
        '.btn-secondary': {
          '@apply inline-flex items-center justify-center px-6 py-3 text-base font-medium': {},
          '@apply text-gray-700 bg-white border-2 border-gray-300 rounded-lg': {},
          '@apply hover:bg-gray-50 hover:border-gray-400': {},
          '@apply focus:outline-none focus:ring-4 focus:ring-gray-200': {},
          '@apply transform hover:scale-105 active:scale-95': {},
          '@apply transition-all duration-200 ease-out': {},
        },
        '.btn-ghost': {
          '@apply inline-flex items-center justify-center px-6 py-3 text-base font-medium': {},
          '@apply text-gray-600 bg-transparent hover:bg-gray-100 rounded-lg': {},
          '@apply focus:outline-none focus:ring-4 focus:ring-gray-200': {},
          '@apply transform hover:scale-105 active:scale-95': {},
          '@apply transition-all duration-200 ease-out': {},
        },

        // CARD COMPONENTS
        '.card-elevated': {
          '@apply bg-white rounded-2xl shadow-lg border border-gray-100': {},
          '@apply hover:shadow-xl transition-shadow duration-300': {},
        },
        '.card-flat': {
          '@apply bg-white rounded-lg border border-gray-200': {},
          '@apply hover:border-gray-300 transition-colors duration-200': {},
        },

        // LOADING STATES
        '.skeleton': {
          '@apply bg-gradient-to-r from-gray-200 via-gray-300 to-gray-200': {},
          '@apply animate-pulse rounded': {},
          backgroundSize: '200% 100%',
          animation: 'skeleton 1.5s ease-in-out infinite',
        },
        
        // UTILITY CLASSES
        '.text-gradient': {
          background: 'linear-gradient(135deg, #0ea5e9 0%, #d946ef 100%)',
          '-webkit-background-clip': 'text',
          '-webkit-text-fill-color': 'transparent',
          'background-clip': 'text',
        },
        '.border-gradient': {
          border: '2px solid transparent',
          background: 'linear-gradient(white, white) padding-box, linear-gradient(135deg, #0ea5e9, #d946ef) border-box',
        },
      })
    }
  ],
}
