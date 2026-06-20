/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ['class'],
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['"Plus Jakarta Sans"', 'system-ui', 'sans-serif'],
      },
      borderRadius: {
        lg: 'var(--radius)',
        md: 'calc(var(--radius) - 2px)',
        sm: 'calc(var(--radius) - 4px)',
      },
      colors: {
        background: 'hsl(var(--background))',
        foreground: 'hsl(var(--foreground))',
        card: {
          DEFAULT: 'hsl(var(--card))',
          foreground: 'hsl(var(--card-foreground))',
        },
        popover: {
          DEFAULT: 'hsl(var(--popover))',
          foreground: 'hsl(var(--popover-foreground))',
        },
        primary: {
          DEFAULT: 'hsl(var(--primary))',
          foreground: 'hsl(var(--primary-foreground))',
        },
        secondary: {
          DEFAULT: 'hsl(var(--secondary))',
          foreground: 'hsl(var(--secondary-foreground))',
        },
        muted: {
          DEFAULT: 'hsl(var(--muted))',
          foreground: 'hsl(var(--muted-foreground))',
        },
        accent: {
          DEFAULT: 'hsl(var(--accent))',
          foreground: 'hsl(var(--accent-foreground))',
        },
        destructive: {
          DEFAULT: 'hsl(var(--destructive))',
          foreground: 'hsl(var(--destructive-foreground))',
        },
        sidebar: {
          DEFAULT: 'hsl(var(--sidebar))',
          foreground: 'hsl(var(--sidebar-foreground))',
          muted: 'hsl(var(--sidebar-muted))',
        },
        status: {
          info: 'hsl(var(--status-info))',
          'info-muted': 'hsl(var(--status-info-muted))',
          warning: 'hsl(var(--status-warning))',
          'warning-muted': 'hsl(var(--status-warning-muted))',
          success: 'hsl(var(--status-success))',
          'success-muted': 'hsl(var(--status-success-muted))',
          error: 'hsl(var(--status-error))',
          'error-muted': 'hsl(var(--status-error-muted))',
          accent: 'hsl(var(--status-accent))',
          'accent-muted': 'hsl(var(--status-accent-muted))',
        },
        border: 'hsl(var(--border))',
        input: 'hsl(var(--input))',
        ring: 'hsl(var(--ring))',
      },
      boxShadow: {
        warm: '0 1px 3px hsl(21 30% 20% / 0.06), 0 4px 16px hsl(21 30% 20% / 0.05)',
        'warm-lg': '0 4px 24px hsl(21 30% 20% / 0.1)',
      },
    },
  },
  plugins: [require('tailwindcss-animate')],
}
