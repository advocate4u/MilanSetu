import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const hasConfiguredApi = Boolean(env.VITE_API_BASE_URL?.trim())

  return {
    plugins: [react()],
    // Relative asset paths work both locally and when hosted under /MilanSetu/ on GitHub Pages.
    base: './',
    // During local development, keep API calls same-origin and let Vite proxy them
    // to the ASP.NET Core HTTPS endpoint. This avoids browser CORS/certificate issues.
    ...(mode === 'development' && !hasConfiguredApi
      ? { define: { 'import.meta.env.VITE_API_BASE_URL': JSON.stringify('/api') } }
      : {}),
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: 'https://localhost:7001',
          changeOrigin: true,
          secure: false,
        },
      },
    },
  }
})
