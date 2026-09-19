import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  return {
    plugins: [react()],
    server: {
      proxy: {
        "/api": {
          target: env.API_PROXY_TARGET || "https://localhost:7051",
          changeOrigin: true,
          // Local ASP.NET development certificates are self-signed.
          secure: false,
        },
      },
    },
  };
});
