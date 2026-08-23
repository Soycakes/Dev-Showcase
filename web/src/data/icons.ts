import {
  siPython, siTypescript, siJavascript, siCplusplus, siSharp, siKotlin,
  siReact, siNextdotjs, siFastapi, siNodedotjs,
  siLangchain,
  siGooglegemini, siGooglesheets,
  siMysql, siSqlite, siElasticsearch, siSupabase,
  siUnity, siUnrealengine, siSteamworks,
  siVercel, siGitlab, siGithub, siGithubactions, siDocker, siPerforce,
  siJetpackcompose,
} from 'simple-icons'
import type { SimpleIcon } from 'simple-icons'

// Maps the stack strings used in projects.ts → simple-icons icon objects.
// Anything not listed here renders as a plain text badge.
export const TECH_ICONS: Record<string, SimpleIcon> = {
  // Languages
  'Python':           siPython,
  'TypeScript':       siTypescript,
  'JavaScript':       siJavascript,
  // Java has no icon in simple-icons v16 — falls back to text badge
  'C++':              siCplusplus,
  'NDK / C++':        siCplusplus,
  'C#':               siSharp,
  'LINQ':             siSharp,        // most relevant
  'Kotlin':           siKotlin,

  // Web / frameworks
  'React':            siReact,
  'Next.js':          siNextdotjs,
  'FastAPI':          siFastapi,
  'Node.js':          siNodedotjs,

  // AI / agents
  'LangGraph':        siLangchain,
  'LangSmith':        siLangchain,
  'Gemini API':       siGooglegemini,
  'Anthropic API':    siGooglegemini, // no Anthropic icon in simple-icons yet

  // Databases / backend
  'MySQL':            siMysql,
  'SQLite':           siSqlite,
  'ElasticSearch':    siElasticsearch,
  'Supabase':         siSupabase,

  // Game engines
  'Unity':            siUnity,
  'ScriptableObjects': siUnity,       // most relevant
  'Unreal Engine 5':  siUnrealengine,
  'Unreal Engine':    siUnrealengine,
  'Blueprints':       siUnrealengine, // most relevant
  'Steamworks':       siSteamworks,

  // DevOps / infra
  'Vercel':           siVercel,
  'GitLab':           siGitlab,
  'GitHub':           siGithub,
  'GitHub Actions':   siGithubactions,
  'Docker':           siDocker,
  'Perforce':         siPerforce,

  // Android
  'Jetpack Compose':  siJetpackcompose,

  // Google
  'AppScript':        siGooglesheets, // most relevant
}
