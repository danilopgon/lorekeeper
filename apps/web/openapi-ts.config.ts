import { readFileSync } from 'node:fs';
import { defineConfig } from '@hey-api/openapi-ts';

const input = '../../services/api/openapi/v1.json';

interface OpenApiDocument {
  paths: Record<string, Record<string, { tags?: string[] }>>;
}

const document = JSON.parse(readFileSync(input, 'utf8')) as OpenApiDocument;

/** One output folder per tag of the product API (`/api/...`), so a new module needs no config change. */
const domainTags = [
  ...new Set(
    Object.entries(document.paths)
      .filter(([path]) => path.startsWith('/api/'))
      .flatMap(([, operations]) =>
        Object.values(operations).flatMap((operation) => operation.tags ?? []),
      ),
  ),
].sort();

export default defineConfig(
  domainTags.map((tag) => ({
    input,
    output: `src/app/shared/api/generated/${tag.toLowerCase()}`,
    parser: { filters: { tags: { include: [tag] } } },
    plugins: ['@hey-api/typescript', { name: 'zod', dates: { offset: true } }],
  })),
);
