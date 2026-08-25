const RAW_FILES = import.meta.glob(
  [
    '../../../*/**/*.{py,cs,cpp,cc,cxx,h,hpp,ts,tsx,js,jsx,java,sql,lua,sh,json,yaml,yml}',
    '!../../../web/**',
    '!../../../.git/**',
    '!../../../.github/**',
    '!../../../.claude/**',
  ],
  { query: '?raw', import: 'default', eager: true }
) as Record<string, string>

const EXT_LANG: Record<string, string> = {
  py: 'python',
  cs: 'csharp',
  cpp: 'cpp', cc: 'cpp', cxx: 'cpp',
  h: 'cpp', hpp: 'cpp',
  ts: 'typescript', tsx: 'typescript',
  js: 'javascript', jsx: 'javascript',
  java: 'java',
  sql: 'sql',
  lua: 'lua',
  sh: 'bash',
  json: 'json',
  yaml: 'yaml', yml: 'yaml',
}

const ALLOWED_EXTS = new Set(Object.keys(EXT_LANG))

export interface DiscoveredSnippet {
  filename: string
  relativePath: string
  language: string
  code: string
}

export function getSnippetsForDir(dir: string): DiscoveredSnippet[] {
  const prefix = `../../../${dir}/`
  return Object.entries(RAW_FILES)
    .filter(([path]) => {
      if (!path.startsWith(prefix)) return false
      const filename = path.split('/').pop()!
      if (filename.startsWith('.')) return false
      const ext = filename.split('.').pop()?.toLowerCase() ?? ''
      return ALLOWED_EXTS.has(ext)
    })
    .map(([path, code]) => {
      const filename = path.split('/').pop()!
      const relativePath = path.slice(prefix.length)
      const ext = filename.split('.').pop()?.toLowerCase() ?? ''
      return { filename, relativePath, language: EXT_LANG[ext] ?? ext, code }
    })
    .sort((a, b) => a.relativePath.localeCompare(b.relativePath))
}
