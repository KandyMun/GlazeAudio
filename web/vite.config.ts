import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// In development the API runs on http://localhost:5080; requests to /api are proxied there,
// so the app can call relative URLs. In production VITE_API_URL points to the App Service.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: { "/api": "http://localhost:5080" },
  },
});
