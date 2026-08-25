import {
  siPython, siTypescript, siJavascript, siCplusplus, siOpenjdk, siKotlin,
  siReact, siNextdotjs, siFastapi, siNodedotjs,
  siLangchain,
  siGooglegemini, siGooglesheets,
  siMysql, siSqlite, siElasticsearch, siSupabase,
  siUnity, siUnrealengine, siSteam,
  siVercel, siGitlab, siGithub, siDocker, siPerforce, siYoutube,
  siJetpackcompose, siAndroid,
} from 'simple-icons'
import type { SimpleIcon } from 'simple-icons'
import csharpUrl from '../assets/CSharp.svg'
import javaUrl from '../assets/Java.svg'

export const CUSTOM_ICONS: Record<string, string> = {
  'C#': csharpUrl,
  'LINQ': csharpUrl,
  'ASP.NET Core MVC': csharpUrl,
  'Java': javaUrl,
}

export const TECH_ICONS: Record<string, SimpleIcon> = {
  'Python': siPython,
  'TypeScript': siTypescript,
  'JavaScript': siJavascript,
  'C++': siCplusplus,
  'NDK / C++': siCplusplus,
  'WebSockets': siCplusplus,
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
  'ECS / DOTS': siUnity,
  'Netcode for Entities': siUnity,
  'Burst Compiler': siUnity,
  'Unreal Engine 5': siUnrealengine,
  'Unreal Engine': siUnrealengine,
  'Blueprints': siUnrealengine,
  'Steamworks': siSteam,

  'Vercel': siVercel,
  'GitLab': siGitlab,
  'GitHub': siGithub,
  'GitHub Actions': siGithub,
  'Docker': siDocker,
  'Perforce': siPerforce,
  'YouTube': siYoutube,

  'Jetpack Compose': siJetpackcompose,
  'Room': siAndroid,
  'AppScript': siGooglesheets,
}
