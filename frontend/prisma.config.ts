import { config } from "dotenv";
import { resolve } from "node:path";
import { defineConfig, env } from "prisma/config";

// Next.js uses .env.local; Prisma CLI reads it here for migrations and introspection.
config({ path: resolve(process.cwd(), ".env.local") });
config({ path: resolve(process.cwd(), ".env") });

export default defineConfig({
  schema: "prisma/schema.prisma",
  migrations: {
    path: "prisma/migrations",
  },
  // Migrations and db pull use the session-mode / direct pooler (port 5432).
  datasource: {
    url: env("DIRECT_URL"),
  },
});
