export type Category =
  | 'ai-agents'
  | 'game-dev'
  | 'fullstack'
  | 'systems'
  | 'tools'

export type Lang = 'en' | 'ko'

export interface L10n {
  en: string
  ko: string
}

export type MediaItem =
  | { kind: 'image'; src: string; alt: string }
  | { kind: 'gif'; src: string; alt: string }
  | { kind: 'youtube'; videoId: string; title: string }
  | { kind: 'demo'; url: string; label: string }

export interface CodeSnippet {
  filename: string
  language: string
  description: L10n
  repoPath?: string
  code: string
}

export interface Project {
  id: string
  title: L10n
  role: L10n
  subtitle: L10n
  period: string
  categories: Category[]
  stack: string[]
  summary: L10n
  bullets: L10n[]
  media: MediaItem[]
  snippets: CodeSnippet[]
  repoUrl?: string
  liveUrl?: string
  store?: { url: string; label: string }
  status: 'shipped' | 'wip' | 'archived'
}
