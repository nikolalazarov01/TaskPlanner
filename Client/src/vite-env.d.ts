/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
  // add other env vars here as needed, e.g.:
  // readonly VITE_ANOTHER_ENV?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}


