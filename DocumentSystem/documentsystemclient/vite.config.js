
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    server: {
        port: 54824,
        proxy: {
            '/api': {
                target: 'https://localhost:7127',
                changeOrigin: true,
                secure: false,
            },
        },
    },
});
