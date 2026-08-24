/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['"Space Grotesk"', 'system-ui', 'sans-serif'],
        mono: ['"Space Mono"', 'monospace'],
      },
      keyframes: {
        wordExit: {
          '0%':   { transform: 'translateY(0)',    opacity: '1' },
          '100%': { transform: 'translateY(110%)', opacity: '0' },
        },
        wordEnter: {
          '0%':   { transform: 'translateY(-110%)', opacity: '0' },
          '100%': { transform: 'translateY(0)',      opacity: '1' },
        },
        fadeIn: {
          '0%':   { opacity: '0', transform: 'translateY(-6px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
      },
      animation: {
        // 'backwards' fill-mode keeps incoming word hidden above during stagger delay
        'word-exit':  'wordExit 0.28s ease-in forwards',
        'word-enter': 'wordEnter 0.28s ease-out backwards',
        'fade-in':    'fadeIn 0.25s ease-out forwards',
      },
    },
  },
  plugins: [require('@tailwindcss/typography')],
}
