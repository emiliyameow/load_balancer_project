import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const balancerTarget = env.BALANCER_API_TARGET || "http://localhost:5120";
  const serverOneTarget = env.BALANCER_SERVER_1_TARGET || "http://localhost:5101";
  const serverTwoTarget = env.BALANCER_SERVER_2_TARGET || "http://localhost:5102";

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        "/api": {
          target: balancerTarget,
          changeOrigin: true
        },
        "/lb": {
          target: balancerTarget,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/lb/, "") || "/"
        },
        "/backend/server-1": {
          target: serverOneTarget,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/backend\/server-1/, "") || "/"
        },
        "/backend/server-2": {
          target: serverTwoTarget,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/backend\/server-2/, "") || "/"
        }
      }
    }
  };
});
