import { useEffect, useRef, useState } from 'react'
import { WordRoller } from './WordRoller'
import { TechBadge } from './TechIcon'
import { MediaSlide, MediaThumb } from './MediaSlide'
import { useLocale } from '../hooks/useLocale'
import { STATUS_LABEL } from '../data/status'
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
      <div className="relative flex-1 min-h-48 rounded-lg overflow-hidden bg-transparent">
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
  onCodeClick?: () => void
}

export function ProjectCard({ project, onCodeClick }: Props) {
  const { t, lang } = useLocale()
  const cardRef = useRef<HTMLElement>(null)
  const [heroLoaded, setHeroLoaded] = useState(false)

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

  const hasRepo = !!project.repoUrl
  const hasLive = !!project.liveUrl
  const hasStore = !!project.store
  const hasSnippets = project.snippets.length > 0 || !!project.snippetsDir

  return (
    <article ref={cardRef} className="rounded-2xl border border-neutral-200 dark:border-neutral-800 bg-white/80 dark:bg-neutral-950/80 overflow-hidden">
      <div className="flex flex-col lg:flex-row">
        <div className="flex flex-col gap-3 p-6 lg:w-[55%]">
          <div>
            <div className="flex items-center justify-between gap-2">
              <h2 className="text-2xl font-bold text-neutral-900 dark:text-white leading-tight">
                <WordRoller text={t(project.title)} />
              </h2>
              <span className="text-xs text-neutral-400 dark:text-neutral-500 shrink-0">
                <WordRoller text={STATUS_LABEL[lang][project.status]} />
              </span>
            </div>
            <p className="text-sm text-neutral-500 dark:text-neutral-400 mt-0.5">
              <WordRoller text={t(project.role)} />, <WordRoller text={t(project.subtitle)} />
            </p>
            <p className="text-xs text-neutral-400 dark:text-neutral-500 mt-0.5">{project.period}</p>
          </div>

          <div className="flex flex-wrap gap-1.5 mt-auto pt-1">
            {project.stack.map(s => <TechBadge key={s} name={s} />)}
          </div>

          <div className="flex flex-wrap gap-2">
            {hasRepo && (
              <a href={project.repoUrl} target="_blank" rel="noreferrer" onClick={e => e.stopPropagation()}
                className="font-mono text-xs bg-neutral-900 dark:bg-white text-white dark:text-neutral-900 px-3 py-1.5 rounded-md hover:opacity-80 transition-opacity">
                [ Github ]
              </a>
            )}
            {hasLive && (
              <a href={project.liveUrl} target="_blank" rel="noreferrer" onClick={e => e.stopPropagation()}
                className="font-mono text-xs bg-neutral-900 dark:bg-white text-white dark:text-neutral-900 px-3 py-1.5 rounded-md hover:opacity-80 transition-opacity">
                [ Live Demo ]
              </a>
            )}
            {hasStore && (
              <a href={project.store!.url} target="_blank" rel="noreferrer" onClick={e => e.stopPropagation()}
                className="font-mono text-xs bg-neutral-900 dark:bg-white text-white dark:text-neutral-900 px-3 py-1.5 rounded-md hover:opacity-80 transition-opacity">
                [ {project.store!.label} ]
              </a>
            )}
            {hasSnippets && onCodeClick && (
              <button onClick={e => { e.stopPropagation(); onCodeClick() }}
                className="font-mono text-xs bg-neutral-900 dark:bg-white text-white dark:text-neutral-900 px-3 py-1.5 rounded-md hover:opacity-80 transition-opacity">
                [ Code Examples ]
              </button>
            )}
          </div>
        </div>

        <div className="p-4 lg:w-[45%]">
          <MediaCarousel items={project.media} eager={heroLoaded} />
        </div>
      </div>

    </article>
  )
}
