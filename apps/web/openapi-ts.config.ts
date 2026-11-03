import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: '../../services/api/openapi/v1.json',
  output: 'src/app/shared/api/generated',
  plugins: ['@hey-api/typescript', { name: 'zod', dates: { offset: true } }],
});
