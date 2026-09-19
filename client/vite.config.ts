import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  return {
    plugins: [react()],
    server: {
      proxy: {
        "/api": {
          target: env.API_PROXY_TARGET || "http://127.0.0.1:5103",
          changeOrigin: true,
          followRedirects: true,
          // Local ASP.NET development certificates are self-signed.
          secure: false,
          configure(proxy) {
            proxy.on("error", (_error, _request, response) => {
              if ("writeHead" in response && !response.headersSent && !response.writableEnded) {
                response.writeHead(503, { "Content-Type": "application/json; charset=utf-8" });
                response.end(JSON.stringify({
                  message: "服務目前無法連線，請稍後再試。",
                }));
              }
            });
          },
        },
      },
    },
  };
});
