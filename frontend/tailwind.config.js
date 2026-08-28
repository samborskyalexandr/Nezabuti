/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        memorial: {
          bg: '#F3EEE6',
          surface: '#FAF7F1',
          ink: '#2C2A27',
          muted: '#6E6A63',
          line: '#E2DCD2',
          accent: '#4F5A4C'
        }
      },
      fontFamily: {
        serif: ['"Cormorant Garamond"', 'Georgia', 'serif'],
        sans: ['Manrope', 'system-ui', 'sans-serif']
      },
      maxWidth: {
        prose: '40rem',
        memorial: '42rem'
      }
    }
  },
  plugins: []
};
