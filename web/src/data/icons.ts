import {
  siPython, siTypescript, siJavascript, siCplusplus, siSharp, siOpenjdk, siKotlin,
  siReact, siNextdotjs, siFastapi, siNodedotjs,
  siLangchain,
  siGooglegemini, siGooglesheets,
  siMysql, siSqlite, siElasticsearch, siSupabase,
  siUnity, siUnrealengine, siSteamworks,
  siVercel, siGitlab, siGithub, siGithubactions, siDocker, siPerforce, siYoutube,
  siJetpackcompose,
} from 'simple-icons'
import type { SimpleIcon } from 'simple-icons'

export const TECH_ICONS: Record<string, SimpleIcon> = {
  'Python': siPython,
  'TypeScript': siTypescript,
  'JavaScript': siJavascript,
  'C++': siCplusplus,
  'NDK / C++': siCplusplus,
  'C#': siSharp,
  'LINQ': siSharp,
  'Java': siOpenjdk,
  'Kotlin': siKotlin,

  'React': siReact,
  'Next.js': siNextdotjs,
  'FastAPI': siFastapi,
  'Node.js': siNodedotjs,

  'LangGraph': siLangchain,
  'LangSmith': siLangchain,
  'Gemini API': siGooglegemini,
  'Anthropic API': siGooglegemini,

  'MySQL': siMysql,
  'SQLite': siSqlite,
  'ElasticSearch': siElasticsearch,
  'Supabase': siSupabase,

  'Unity': siUnity,
  'ScriptableObjects': siUnity,
  'Unreal Engine 5': siUnrealengine,
  'Unreal Engine': siUnrealengine,
  'Blueprints': siUnrealengine,
  'Steamworks': siSteamworks,

  'Vercel': siVercel,
  'GitLab': siGitlab,
  'GitHub': siGithub,
  'GitHub Actions': siGithubactions,
  'Docker': siDocker,
  'Perforce': siPerforce,
  'YouTube': siYoutube,

  'Jetpack Compose': siJetpackcompose,
  'AppScript': siGooglesheets,
}
