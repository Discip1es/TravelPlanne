import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// В режиме разработки запросы /api и /hubs проксируются на ASP.NET Core,
// поэтому CORS и cookie-проблем не возникает. Сборка кладётся прямо в wwwroot бэкенда.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5095,
    strictPort: true,
    proxy: {
      '/api': { target: 'http://localhost:5080', changeOrigin: true },
      '/health': { target: 'http://localhost:5080', changeOrigin: true },
      '/hubs': { target: 'http://localhost:5080', ws: true, changeOrigin: true }
    }
  },
  build: {
    outDir: '../src/TravelPlanner.Api/wwwroot',
    emptyOutDir: true,
    sourcemap: false
  }
});