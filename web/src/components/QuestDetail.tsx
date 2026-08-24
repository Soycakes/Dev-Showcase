import 'highlight.js/styles/github-dark.css'
import { useRef, useEffect, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import rehypeHighlight from 'rehype-highlight'
import { WordRoller } from './WordRoller'
import { TechBadge } from './TechIcon'
import { MediaSlide, MediaThumb } from './MediaSlide'
import { useLocale } from '../hooks/useLocale'
import { STATUS_STYLE, STATUS_LABEL } from '../data/status'
import type { MediaItem, Project } from '../data/types'

const mdFiles = import.meta.glob('../content/projects/*.md', {
  query: '?raw',
  import: 'default',
  eager: true,
}) as Record<string, string>

function getMarkdown(id: string): string {
  return mdFiles[`../content/projects/${id}.md`] ?? ''
}


const BTN = 'font-mono text-xs border border-neutral-400 dark:border-neutral-600 px-3 py-1.5 rounded hover:bg-neutral-100 dark:hover:bg-neutral-800 transition-colors text-neutral-700 dark:text-neutral-300'

interface Props {
  project: Project
  prevProject: Project | null
  nextProject: Project | null
  onPrev: () => void
  onNext: () => void
  onBack: () => void
}

export function QuestDetail({ project, prevProject, nextProject, onPrev, onNext, onBack }: Props) {
  const { t, lang } = useLocale()
  const scrollRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    scrollRef.current?.scrollTo(0, 0)
  }, [project.id])

  const markdown = getMarkdown(project.id)
  const hasRepo = !!project.repoUrl
  const hasLive = !!project.liveUrl
  const hasSnippets = project.snippets.length > 0

  return (
    <div ref={scrollRef} className="h-full overflow-y-auto bg-white/50 dark:bg-neutral-950/50">
      <div className="max-w-3xl mx-auto px-6 py-8">
        <div className="flex flex-wrap items-start justify-between gap-4 mb-6">
          <div>
            <span className={`inline-block text-xs font-medium px-2 py-0.5 rounded-full mb-2 ${STATUS_STYLE[project.status]}`}>
              {STATUS_LABEL[lang][project.status]}
            </span>
            <h1 className="text-2xl font-bold text-neutral-900 dark:text-white">
              <WordRoller text={t(project.title)} single />
            </h1>
            <p className="text-sm text-neutral-500 dark:text-neutral-400 mt-1">
              <WordRoller text={t(project.subtitle)} single /> - {project.period}
            </p>
          </div>
          <div className="flex items-center gap-2 flex-wrap">
            <button onClick={onBack} className={`lg:hidden ${BTN}`}>[ Overview ]</button>
            {prevProject && <button onClick={onPrev} className={BTN}>{`<< Prev`}</button>}
            {nextProject && <button onClick={onNext} className={BTN}>{`Next >>`}</button>}
          </div>
        </div>

        <div className="flex flex-wrap gap-2 mb-8">
          {hasLive && (
            <a href={project.liveUrl} target="_blank" rel="noreferrer" className={BTN}>
              [ Live Demo ]
            </a>
          )}
          {hasRepo && (
            <a href={project.repoUrl} target="_blank" rel="noreferrer" className={BTN}>
              [ Repo ]
            </a>
          )}
          {hasSnippets && (
            <span className="font-mono text-xs border border-neutral-200 dark:border-neutral-700 px-3 py-1.5 rounded text-neutral-400">
              [ {project.snippets.length} code files below ]
            </span>
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

        {hasSnippets && (
          <div className="space-y-4 mb-8">
            {project.snippets.map((snippet, i) => (
              <div key={i} className="rounded-lg border border-neutral-200 dark:border-neutral-800 overflow-hidden">
                <div className="flex items-center justify-between px-4 py-2 bg-neutral-50 dark:bg-neutral-900 border-b border-neutral-200 dark:border-neutral-800">
                  <span className="text-xs font-mono text-neutral-500">{snippet.filename}</span>
                  <span className="text-xs text-neutral-400">{snippet.language}</span>
                </div>
                <p className="px-4 py-2 text-xs text-neutral-500 dark:text-neutral-400 border-b border-neutral-100 dark:border-neutral-800">
                  <WordRoller text={t(snippet.description)} />
                </p>
                <pre className="overflow-x-auto px-4 py-4 text-xs leading-relaxed font-mono text-neutral-200 bg-neutral-950">
                  <code>{snippet.code}</code>
                </pre>
              </div>
            ))}
          </div>
        )}

        <div className="flex flex-wrap gap-1.5">
          {project.stack.map(s => <TechBadge key={s} name={s} />)}
        </div>
      </div>
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

