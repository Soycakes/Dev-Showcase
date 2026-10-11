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
      <div className="flex h-full min-h-48 items-center justify-center rounded-sm bg-neutral-100 dark:bg-neutral-800 text-xs text-neutral-400">
        Media coming soon
      </div>
    )
  }

  function prev() { setIdx(i => (i - 1 + items.length) % items.length) }
  function next() { setIdx(i => (i + 1) % items.length) }

  return (
    <div className="flex flex-col gap-2 h-full">
      <div className="relative flex-1 min-h-48 rounded-sm overflow-hidden bg-transparent">
        {eager ? <MediaSlide item={current} /> : (
          <div className="absolute inset-0 animate-pulse bg-neutral-200 dark:bg-neutral-700" />
        )}
        {items.length > 1 && (
          <>
            <button onClick={prev} className="absolute left-2 top-1/2 -translate-y-1/2 bg-black/40 px-2 py-1 text-white text-xs hover:bg-black/60">&#8249;</button>
            <button onClick={next} className="absolute right-2 top-1/2 -translate-y-1/2 bg-black/40 px-2 py-1 text-white text-xs hover:bg-black/60">&#8250;</button>
            <span className="absolute bottom-2 left-1/2 -translate-x-1/2 bg-black/40 px-2 py-0.5 text-xs text-white">{idx + 1} / {items.length}</span>
          </>
        )}
      </div>
      {items.length > 1 && (
        <div className="flex gap-1.5 overflow-x-auto pb-0.5">
          {items.map((item, i) => (
            <button
              key={i}
              onClick={() => setIdx(i)}
              className={`h-12 w-16 shrink-0 overflow-hidden transition-opacity ${i === idx ? 'opacity-100' : 'opacity-40 hover:opacity-70'}`}
            >
              <MediaThumb item={item} />
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

// first action in a BTN_ROW renders solid, the rest outlined
export const BTN = 'border border-neutral-300 px-3 py-1.5 font-mono text-xs text-neutral-700 transition-colors hover:border-neutral-900 hover:text-neutral-900 dark:border-neutral-700 dark:text-neutral-300 dark:hover:border-neutral-300 dark:hover:text-white'
export const BTN_ROW = 'flex flex-wrap gap-2 [&>:first-child]:border-neutral-900 [&>:first-child]:bg-neutral-900 [&>:first-child]:text-white [&>:first-child:hover]:bg-neutral-700 [&>:first-child:hover]:text-white dark:[&>:first-child]:border-white dark:[&>:first-child]:bg-white dark:[&>:first-child]:text-neutral-900 dark:[&>:first-child:hover]:bg-neutral-200 dark:[&>:first-child:hover]:text-neutral-900'

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
    <article ref={cardRef} className="border-t border-neutral-900/15 py-8 dark:border-white/15">
      <div className="flex flex-col gap-6 lg:flex-row lg:gap-10">
        <div className={`flex flex-col gap-3 ${project.featured ? 'lg:w-[45%]' : 'lg:w-[55%]'}`}>
          <div>
            <div className="flex items-center justify-between gap-2">
              <h2 className={`font-bold tracking-tight text-neutral-900 dark:text-white leading-tight ${project.featured ? 'text-3xl' : 'text-2xl'}`}>
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

          {project.stats && (
            <dl className="grid grid-cols-4 gap-2 border-y border-neutral-200 dark:border-neutral-800 py-3">
              {project.stats.map(st => (
                <div key={st.value}>
                  <dt className="text-xl font-bold text-neutral-900 dark:text-white">{st.value}</dt>
                  <dd className="text-[11px] leading-tight text-neutral-500 dark:text-neutral-400">{t(st.label)}</dd>
                </div>
              ))}
            </dl>
          )}

          <div className="flex flex-wrap gap-1.5 mt-auto pt-1">
            {project.stack.map(s => <TechBadge key={s} name={s} />)}
          </div>

          <div className={`${BTN_ROW} pt-1`}>
            {hasLive && (
              <a href={project.liveUrl} target="_blank" rel="noreferrer" onClick={e => e.stopPropagation()}
                className={BTN}>
                Live demo ↗
              </a>
            )}
            {hasStore && (
              <a href={project.store!.url} target="_blank" rel="noreferrer" onClick={e => e.stopPropagation()}
                className={BTN}>
                {project.store!.label} ↗
              </a>
            )}
            {hasRepo && (
              <a href={project.repoUrl} target="_blank" rel="noreferrer" onClick={e => e.stopPropagation()}
                className={BTN}>
                GitHub ↗
              </a>
            )}
            {hasSnippets && onCodeClick && (
              <button onClick={e => { e.stopPropagation(); onCodeClick() }}
                className={BTN}>
                Code examples →
              </button>
            )}
          </div>
        </div>

        <div className={project.featured ? 'lg:w-[55%]' : 'lg:w-[45%]'}>
          <MediaCarousel items={project.media} eager={heroLoaded} />
        </div>
      </div>

    </article>
  )
}
