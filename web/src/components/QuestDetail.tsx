import 'highlight.js/styles/github-dark.css'
import { useRef, useEffect, useState, useMemo } from 'react'
import folderOpenUrl from '../assets/icon_folderOpen.svg'
import folderClosedUrl from '../assets/icon_folderClosed.svg'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import rehypeHighlight from 'rehype-highlight'
import hljs from 'highlight.js'
import { WordRoller } from './WordRoller'
import { TechBadge } from './TechIcon'
import { MediaSlide, MediaThumb } from './MediaSlide'
import { useLocale } from '../hooks/useLocale'
import { STATUS_LABEL } from '../data/status'
import { getSnippetsForDir } from '../data/codeFiles'
import type { MediaItem, Project } from '../data/types'

const mdFiles = import.meta.glob('../content/projects/*.md', {
  query: '?raw',
  import: 'default',
  eager: true,
}) as Record<string, string>

function getMarkdown(id: string, lang: string): string {
  return mdFiles[`../content/projects/${id}.${lang}.md`]
    ?? mdFiles[`../content/projects/${id}.en.md`]
    ?? ''
}


const BTN_PRIMARY = 'font-mono text-xs bg-neutral-900 dark:bg-white text-white dark:text-neutral-900 px-3 py-1.5 rounded-md hover:opacity-80 transition-opacity'
const BTN_NAV = 'font-mono text-xs border border-neutral-300 dark:border-neutral-700 px-3 py-1.5 rounded-md text-neutral-600 dark:text-neutral-400 hover:bg-neutral-100 dark:hover:bg-neutral-800 transition-colors'

interface Props {
  project: Project
  prevProject: Project | null
  nextProject: Project | null
  onPrev: () => void
  onNext: () => void
  onBack: () => void
  scrollToCode?: boolean
  onScrollCodeDone?: () => void
}

export function QuestDetail({ project, prevProject, nextProject, onPrev, onNext, onBack, scrollToCode, onScrollCodeDone }: Props) {
  const { t, lang } = useLocale()
  const scrollRef = useRef<HTMLDivElement>(null)
  const snippetsRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    scrollRef.current?.scrollTo(0, 0)
  }, [project.id])

  function scrollToSnippets() {
    if (!snippetsRef.current || !scrollRef.current) return
    const containerRect = scrollRef.current.getBoundingClientRect()
    const elemRect = snippetsRef.current.getBoundingClientRect()
    scrollRef.current.scrollBy({ top: elemRect.top - containerRect.top - 80, behavior: 'smooth' })
  }

  useEffect(() => {
    if (!scrollToCode) return
    const timer = setTimeout(() => {
      scrollToSnippets()
      onScrollCodeDone?.()
    }, 250)
    return () => clearTimeout(timer)
  }, [scrollToCode])

  const markdown = getMarkdown(project.id, lang)
  const hasRepo = !!project.repoUrl
  const hasLive = !!project.liveUrl
  const hasStore = !!project.store
  const snippets = [
    ...project.snippets,
    ...(project.snippetsDir ? getSnippetsForDir(project.snippetsDir) : []),
  ]
  const hasSnippets = snippets.length > 0

  return (
    <div ref={scrollRef} className="h-full overflow-y-auto bg-white/50 dark:bg-neutral-950/50">
      <div className="px-6 py-8">
        <div className="flex flex-wrap items-start justify-between gap-4 mb-6">
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-2xl font-bold text-neutral-900 dark:text-white">
                <WordRoller text={t(project.title)} />
              </h1>
              <span className="text-xs text-neutral-400 dark:text-neutral-500 shrink-0">
                <WordRoller text={STATUS_LABEL[lang][project.status]} />
              </span>
            </div>
            <p className="text-sm text-neutral-500 dark:text-neutral-400 mt-1">
              <WordRoller text={t(project.role)} />, <WordRoller text={t(project.subtitle)} />
            </p>
            <p className="text-xs text-neutral-400 dark:text-neutral-500 mt-0.5">{project.period}</p>
          </div>
          <div className="flex items-center gap-2 flex-wrap">
            <button onClick={onBack} className={`lg:hidden ${BTN_NAV}`}>[ <WordRoller text={lang === 'en' ? 'Overview' : '목록'} /> ]</button>
            {prevProject && <button onClick={onPrev} className={BTN_NAV}>{`<< Prev`}</button>}
            {nextProject && <button onClick={onNext} className={BTN_NAV}>{`Next >>`}</button>}
          </div>
        </div>

        <div className="flex flex-wrap gap-2 mb-8">
          {hasLive && (
            <a href={project.liveUrl} target="_blank" rel="noreferrer" className={BTN_PRIMARY}>
              [ Live Demo ]
            </a>
          )}
          {hasStore && (
            <a href={project.store!.url} target="_blank" rel="noreferrer" className={BTN_PRIMARY}>
              [ {project.store!.label} ]
            </a>
          )}
          {hasRepo && (
            <a href={project.repoUrl} target="_blank" rel="noreferrer" className={BTN_PRIMARY}>
              [ Github ]
            </a>
          )}
          {hasSnippets && (
            <button onClick={scrollToSnippets} className={BTN_PRIMARY}>
              [ Code Examples ]
            </button>
          )}
        </div>

        {project.media.length > 0 && (
          <div className="mb-8 rounded-xl overflow-hidden border border-neutral-200 dark:border-neutral-800">
            <DetailMediaCarousel items={project.media} />
          </div>
        )}

        <div className="prose prose-neutral dark:prose-invert prose-sm max-w-none mb-8">
          <ReactMarkdown remarkPlugins={[remarkGfm]} rehypePlugins={[rehypeHighlight]}>
            {markdown}
          </ReactMarkdown>
        </div>

        {hasSnippets && <div ref={snippetsRef}><SnippetTree snippets={snippets} /></div>}

        <div className="flex flex-wrap gap-1.5">
          {project.stack.map(s => <TechBadge key={s} name={s} />)}
        </div>
      </div>
    </div>
  )
}

type AnySnippet = { filename: string; relativePath?: string; language: string; code: string; description?: { en: string; ko: string } }

type FileNode = { kind: 'file'; name: string; snippet: AnySnippet }
type FolderNode = { kind: 'folder'; name: string; folderPath: string; children: TreeEntry[] }
type TreeEntry = FileNode | FolderNode

function insertIntoTree(entries: TreeEntry[], parts: string[], snippet: AnySnippet, parentPath: string): void {
  if (parts.length === 1) {
    entries.push({ kind: 'file', name: parts[0], snippet })
    return
  }
  const folderName = parts[0]
  const folderPath = parentPath ? `${parentPath}/${folderName}` : folderName
  let folder = entries.find(e => e.kind === 'folder' && e.name === folderName) as FolderNode | undefined
  if (!folder) {
    folder = { kind: 'folder', name: folderName, folderPath, children: [] }
    entries.push(folder)
  }
  insertIntoTree(folder.children, parts.slice(1), snippet, folderPath)
}

function buildTree(snippets: AnySnippet[]): TreeEntry[] {
  const root: TreeEntry[] = []
  for (const s of snippets) {
    insertIntoTree(root, (s.relativePath ?? s.filename).split('/'), s, '')
  }
  return root
}

function firstFile(entries: TreeEntry[]): AnySnippet | null {
  for (const e of entries) {
    if (e.kind === 'file') return e.snippet
    const f = firstFile(e.children)
    if (f) return f
  }
  return null
}

function TreeNodes({ entries, collapsed, onToggle, activePath, onSelect, depth }: {
  entries: TreeEntry[]
  collapsed: Set<string>
  onToggle: (path: string) => void
  activePath: string
  onSelect: (snippet: AnySnippet) => void
  depth: number
}) {
  return (
    <>
      {entries.map(entry => {
        if (entry.kind === 'file') {
          const path = entry.snippet.relativePath ?? entry.snippet.filename
          const isActive = path === activePath
          return (
            <button
              key={entry.name}
              onClick={() => onSelect(entry.snippet)}
              style={{ paddingLeft: `${10 + depth * 16}px` }}
              className={`flex items-center w-full text-left py-1 pr-3 text-xs font-mono transition-colors ${isActive ? 'bg-neutral-700 text-white' : 'text-neutral-200 hover:text-white'}`}
            >
              <span className="w-4 h-4 mr-1.5 shrink-0" />
              {entry.name}
            </button>
          )
        }
        const isCollapsed = collapsed.has(entry.folderPath)
        return (
          <div key={entry.name}>
            <button
              onClick={() => onToggle(entry.folderPath)}
              style={{ paddingLeft: `${10 + depth * 16}px` }}
              className="flex items-center w-full text-left py-1 pr-3 text-xs font-mono text-neutral-300 hover:text-white transition-colors"
            >
              <img src={isCollapsed ? folderClosedUrl : folderOpenUrl} className="w-4 h-4 mr-1.5 shrink-0" alt="" />
              {entry.name}
            </button>
            {!isCollapsed && (
              <TreeNodes entries={entry.children} collapsed={collapsed} onToggle={onToggle} activePath={activePath} onSelect={onSelect} depth={depth + 1} />
            )}
          </div>
        )
      })}
    </>
  )
}

function SnippetTree({ snippets }: { snippets: AnySnippet[] }) {
  const { t } = useLocale()
  const tree = useMemo<TreeEntry[]>(() => [
    { kind: 'folder', name: 'Code Examples', folderPath: '__root__', children: buildTree(snippets) }
  ], [snippets])
  const [current, setCurrent] = useState<AnySnippet>(() => firstFile(tree) ?? snippets[0])
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set())
  const [copied, setCopied] = useState(false)

  const activePath = current.relativePath ?? current.filename

  function toggleFolder(path: string) {
    setCollapsed(prev => {
      const next = new Set(prev)
      next.has(path) ? next.delete(path) : next.add(path)
      return next
    })
  }

  const highlighted = hljs.getLanguage(current.language)
    ? hljs.highlight(current.code, { language: current.language }).value
    : hljs.highlightAuto(current.code).value

  function copy() {
    navigator.clipboard.writeText(current.code)
    setCopied(true)
    setTimeout(() => setCopied(false), 1500)
  }

  return (
    <div className="mb-8 rounded-lg border border-neutral-200 dark:border-neutral-800 overflow-hidden">
      <div className="border-b border-neutral-200 dark:border-neutral-800 bg-neutral-950/20 py-1">
        <TreeNodes entries={tree} collapsed={collapsed} onToggle={toggleFolder} activePath={activePath} onSelect={setCurrent} depth={0} />
      </div>
      {current.description && (
        <p className="px-4 py-2 text-xs text-neutral-500 dark:text-neutral-400 border-b border-neutral-100 dark:border-neutral-800 bg-neutral-50 dark:bg-neutral-900">
          <WordRoller text={t(current.description)} />
        </p>
      )}
      <div className="flex items-center justify-between border-b border-neutral-200 dark:border-neutral-800 bg-neutral-900 px-4 py-2">
        <span className="text-xs font-mono text-neutral-400">{current.filename}</span>
        <button onClick={copy} className="text-xs font-mono text-neutral-400 hover:text-white transition-colors">
          {copied ? 'Copied!' : 'Copy'}
        </button>
      </div>
      <pre className="overflow-x-auto px-4 py-4 text-xs leading-relaxed font-mono bg-neutral-950">
        <code className="hljs" dangerouslySetInnerHTML={{ __html: highlighted }} />
      </pre>
    </div>
  )
}

function DetailMediaCarousel({ items }: { items: MediaItem[] }) {
  const [idx, setIdx] = useState(0)
  const current = items[idx]

  function prev() { setIdx(i => (i - 1 + items.length) % items.length) }
  function next() { setIdx(i => (i + 1) % items.length) }

  return (
    <div>
      <div className="relative aspect-video bg-neutral-100 dark:bg-neutral-800">
        <MediaSlide item={current} />
        {items.length > 1 && (
          <>
            <button onClick={prev} className="absolute left-3 top-1/2 -translate-y-1/2 rounded bg-black/40 px-3 py-1.5 text-white hover:bg-black/60">&#8249;</button>
            <button onClick={next} className="absolute right-3 top-1/2 -translate-y-1/2 rounded bg-black/40 px-3 py-1.5 text-white hover:bg-black/60">&#8250;</button>
            <span className="absolute bottom-3 left-1/2 -translate-x-1/2 rounded bg-black/40 px-2 py-0.5 text-xs text-white">{idx + 1} / {items.length}</span>
          </>
        )}
      </div>
      {items.length > 1 && (
        <div className="flex gap-2 p-3 overflow-x-auto bg-neutral-50 dark:bg-neutral-900">
          {items.map((item, i) => (
            <button key={i} onClick={() => setIdx(i)}
              className={`h-14 w-20 shrink-0 rounded overflow-hidden border-2 transition-colors ${i === idx ? 'border-neutral-900 dark:border-white' : 'border-transparent'}`}>
              <MediaThumb item={item} />
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

