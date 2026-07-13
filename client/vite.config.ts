import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      // Local dev: forward API calls to the ASP.NET Core backend.
      "/api": { target: "http://localhost:5199", changeOrigin: true },
    },
  },
});
