import { mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import * as esbuild from "esbuild";

const rootDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const appDirectory = path.join(rootDirectory, "src", "PcAssistant", "PcAssistant");
const outputDirectory = path.join(appDirectory, "wwwroot", "bundles");

await mkdir(outputDirectory, { recursive: true });

const sharedOptions = {
    bundle: true,
    legalComments: "none",
    logLevel: "info",
    minify: true,
    sourcemap: false,
    target: ["chrome109", "edge109", "firefox109", "safari16"]
};

await Promise.all([
    esbuild.build({
        ...sharedOptions,
        entryPoints: [path.join(appDirectory, "Assets", "app.bundle.css")],
        outfile: path.join(outputDirectory, "app.min.css")
    }),
    esbuild.build({
        ...sharedOptions,
        entryPoints: [path.join(appDirectory, "Assets", "app.bundle.js")],
        format: "iife",
        outfile: path.join(outputDirectory, "app.min.js")
    })
]);
