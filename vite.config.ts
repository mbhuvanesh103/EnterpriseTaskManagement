import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { tanstackStart } from "@tanstack/react-start/plugin/vite";

export default defineConfig({
  plugins: [
    tanstackStart({
      router: {
        generatedRouteTree: "src/routeTree.gen.ts",
      },
    }),
    react(),
    tailwindcss(),
  ],
  server: {
    entry: "server",
  },
  resolve: {
    tsconfigPaths: true,
  },
});
