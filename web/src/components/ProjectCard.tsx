import { useEffect, useRef, useState } from 'react'
import { WordRoller } from './WordRoller'
import { TechBadge } from './TechIcon'
import { MediaSlide, MediaThumb } from './MediaSlide'
import { useLocale } from '../hooks/useLocale'
import { STATUS_STYLE, STATUS_LABEL } from '../data/status'
import type { MediaItem, Project } from '../data/types'

function MediaCarousel({ items, eager }: { items: MediaItem[]; eager: boolean }) {
  const [idx, setIdx] = useState(0)
  const current = items[idx]

  if (items.length === 0) {
    return (
      <div className="flex h-full min-h-48 items-center justify-center rounded-lg bg-neutral-100 dark:bg-neutral-800 text-xs text-neutral-400">
        Media coming soon
      </div>
    )
  }

  function prev() { setIdx(i => (i - 1 + items.length) % items.length) }
  function next() { setIdx(i => (i + 1) % items.length) }

  return (
    <div className="flex flex-col gap-2 h-full">
      <div className="relative flex-1 min-h-48 rounded-lg overflow-hidden bg-neutral-100 dark:bg-neutral-800">
        {eager ? <MediaSlide item={current} /> : (
          <div className="absolute inset-0 animate-pulse bg-neutral-200 dark:bg-neutral-700" />
        )}
        {items.length > 1 && (
          <>
            <button onClick={prev} className="absolute left-2 top-1/2 -translate-y-1/2 rounded bg-black/40 px-2 py-1 text-white text-xs hover:bg-black/60">&#8249;</button>
            <button onClick={next} className="absolute right-2 top-1/2 -translate-y-1/2 rounded bg-black/40 px-2 py-1 text-white text-xs hover:bg-black/60">&#8250;</button>
            <span className="absolute bottom-2 left-1/2 -translate-x-1/2 rounded bg-black/40 px-2 py-0.5 text-xs text-white">{idx + 1} / {items.length}</span>
          </>
        )}
      </div>
      {items.length > 1 && (
        <div className="flex gap-1.5 overflow-x-auto pb-0.5">
          {items.map((item, i) => (
            <button
              key={i}
              onClick={() => setIdx(i)}
              className={`h-12 w-16 shrink-0 rounded overflow-hidden border-2 transition-colors ${i === idx ? 'border-neutral-900 dark:border-white' : 'border-transparent'}`}
            >
              <MediaThumb item={item} />
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

interface Props {
  project: Project
}

export function ProjectCard({ project }: Props) {
  const { t, lang } = useLocale()
  const cardRef = useRef<HTMLElement>(null)
  const [heroLoaded, setHeroLoaded] = useState(false)
  const [codeOpen, setCodeOpen] = useState(false)

  useEffect(() => {
    const el = cardRef.current
    if (!el) return
    const obs = new IntersectionObserver(
      ([entry]) => { if (entry.isIntersecting) { setHeroLoaded(true); obs.disconnect() } },
      { rootMargin: '200px' }
    )
    obs.observe(el)
    return () => obs.disconnect()
  }, [])

  const hasSnippets = project.snippets.length > 0
  const hasRepo = !!project.repoUrl
  const hasLive = !!project.liveUrl

  return (
    <article ref={cardRef} className="rounded-2xl border border-neutral-200 dark:border-neutral-800 bg-white dark:bg-neutral-950 overflow-hidden">
      <div className="flex flex-col lg:flex-row">
        <div className="flex flex-col gap-3 p-6 lg:w-[55%]">
          <div>
            <span className={`inline-block text-xs font-medium px-2 py-0.5 rounded-full mb-2 ${STATUS_STYLE[project.status]}`}>
              {STATUS_LABEL[lang][project.status]}
            </span>
            <h2 className="text-2xl font-bold text-neutral-900 dark:text-white leading-tight">
              <WordRoller text={t(project.title)} single />
            </h2>
            <p className="text-sm text-neutral-500 dark:text-neutral-400 mt-0.5">
              <WordRoller text={t(project.subtitle)} single /> - {project.period}
            </p>
          </div>

          <p className="text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
            <WordRoller text={t(project.summary)} />
          </p>

          {project.bullets.length > 0 && (
            <ul className="space-y-1.5">
              {project.bullets.map((b, i) => (
                <li key={i} className="flex gap-2 text-sm text-neutral-600 dark:text-neutral-400">
                  <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-neutral-400" />
                  <WordRoller text={t(b)} />
                </li>
              ))}
            </ul>
          )}

          <div className="flex flex-wrap gap-1.5 mt-auto pt-1">
            {project.stack.map(s => <TechBadge key={s} name={s} />)}
          </div>

          <div className="flex flex-wrap gap-2">
            {hasSnippets && (
              <button
                onClick={e => { e.stopPropagation(); setCodeOpen(o => !o) }}
                className="font-mono text-xs border border-neutral-400 dark:border-neutral-600 px-3 py-1.5 rounded hover:bg-neutral-100 dark:hover:bg-neutral-800 transition-colors text-neutral-700 dark:text-neutral-300"
              >
                [ View Code ({project.snippets.length}) ]
              </button>
            )}
            {!hasSnippets && hasRepo && (
              <a href={project.repoUrl} target="_blank" rel="noreferrer" onClick={e => e.stopPropagation()}
                className="font-mono text-xs border border-neutral-400 dark:border-neutral-600 px-3 py-1.5 rounded hover:bg-neutral-100 dark:hover:bg-neutral-800 transition-colors text-neutral-700 dark:text-neutral-300">
                [ Repo ]
              </a>
            )}
            {hasLive && (
              <a href={project.liveUrl} target="_blank" rel="noreferrer" onClick={e => e.stopPropagation()}
                className="font-mono text-xs border border-neutral-400 dark:border-neutral-600 px-3 py-1.5 rounded hover:bg-neutral-100 dark:hover:bg-neutral-800 transition-colors text-neutral-700 dark:text-neutral-300">
                [ Live Demo ]
              </a>
            )}
          </div>
        </div>

        <div className="p-4 lg:w-[45%] lg:border-l border-t lg:border-t-0 border-neutral-100 dark:border-neutral-800">
          <MediaCarousel items={project.media} eager={heroLoaded} />
        </div>
      </div>

      {hasSnippets && (
        <div className={`grid transition-[grid-template-rows] duration-300 ease-in-out ${codeOpen ? 'grid-rows-[1fr]' : 'grid-rows-[0fr]'}`}>
          <div className="overflow-hidden">
            <div className="border-t border-neutral-100 dark:border-neutral-800 space-y-3 p-6">
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
          </div>
        </div>
      )}
    </article>
  )
}
