import { defineConfig } from "vite";
import fs from "fs";
import path from "path";
import webapp from "@eusoft/webapp-compiler-rollup";

export default defineConfig(({ mode }) => ({
    server: {
        host: "0.0.0.0",
        port: 4541,
        https: {
            key: fs.readFileSync("./localhost-key.pem"),
            cert: fs.readFileSync("./localhost.pem")
        },
        fs: {
            allow: [path.resolve(import.meta.dirname, "../../../../")]
        }
    },
    preview: {
        host: "0.0.0.0",
        port: 4541,
        https: {
            key: fs.readFileSync("./localhost-key.pem"),
            cert: fs.readFileSync("./localhost.pem")
        }
    },
    esbuild: {
        jsx: "preserve"
    },
    build: {
        outDir: "dist",
        sourcemap: true,
        emptyOutDir: true,
        minify: mode == "development" ? false : undefined,
        rolldownOptions: {
            output: {
                keepNames: true
            }
        }
    },
    publicDir: "public",
    plugins: [webapp()]
}));