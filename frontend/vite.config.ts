import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  // Relative asset paths work both locally and when hosted under /MilanSetu/ on GitHub Pages.
  base: './',
  server: {
    port: 5173
  }
})
