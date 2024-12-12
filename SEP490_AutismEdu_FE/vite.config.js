import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import svgr from 'vite-plugin-svgr'
// https://vitejs.dev/config/
export default defineConfig({
  plugins: [

    react(
      {
        jsxImportSource: '@emotion/react',
        babel: {
          plugins: ['@emotion/babel-plugin'],
        }
      }
    ),
    svgr({
      svgrOptions: {
        icon: true
      }
    })
  ],
  base: '/',
  resolve: {
    alias: [
      { find: '~', replacement: '/src' }
    ]
  },
  server: {
    host: '0.0.0.0',
    port: 5173,
    hmr: {
      host: '20.205.21.17', // Use the public IP address of your VM for WebSockets
      protocol: 'ws',        // WebSocket protocol
      port: 5173,            // Same port as your dev server
    },
  }
})