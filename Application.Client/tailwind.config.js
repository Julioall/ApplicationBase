/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  darkMode: ['class', '[data-theme="dark"]'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'Segoe UI', 'system-ui', '-apple-system', 'sans-serif'],
      },
      colors: {
        body: 'var(--bg-body)',
        page: 'var(--page-bg)',
        surface: 'var(--surface)',
        'surface-alt': 'var(--surface-alt)',
        'surface-muted': 'var(--surface-muted)',
        'surface-raised': 'var(--surface-raised)',
        'surface-strong': 'var(--surface-strong)',
        'surface-elevated': 'var(--surface-elevated)',
        'border-soft': 'var(--border-soft)',
        'border-strong': 'var(--border-strong)',
        'text-primary': 'var(--text-primary)',
        'text-subtle': 'var(--text-subtle)',
        'text-muted': 'var(--text-muted)',
        'text-inverse': 'var(--text-inverse)',
        primary: 'var(--primary)',
        'primary-strong': 'var(--primary-strong)',
        'primary-soft': 'var(--primary-soft)',
        accent: 'var(--accent)',
        'accent-strong': 'var(--accent-strong)',
        'accent-soft': 'var(--accent-soft)',
        success: 'var(--success)',
        warning: 'var(--warning)',
        danger: 'var(--danger)',
        info: 'var(--info)',
        'overlay-strong': 'var(--overlay-strong)',
        'overlay-soft': 'var(--overlay-soft)',
      },
      borderRadius: {
        md: 'var(--radius-md)',
        lg: 'var(--radius-lg)',
        xl: 'var(--radius-xl)',
      },
      spacing: {
        control: 'var(--control-height)',
      },
      boxShadow: {
        soft: 'var(--shadow-soft)',
        focus: '0 0 0 3px rgba(var(--focus-rgb), 0.35)',
      },
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
