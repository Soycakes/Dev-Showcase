import { useLocale } from '../hooks/useLocale'
import { FilterBar } from './FilterBar'
import { WordRoller } from './WordRoller'
import { TechBadge } from './TechIcon'
import { MediaThumb } from './MediaSlide'
import type { Category, Project } from '../data/types'

interface RailItemProps {
  project: Project
  isActive: boolean
  onClick: () => void
}

function RailItem({ project, isActive, onClick }: RailItemProps) {
  const { t } = useLocale()
  const thumb = project.media[0] ?? null

  return (
    <button
      onClick={onClick}
      className={`w-full text-left px-4 py-3 border-b border-neutral-100 dark:border-neutral-800 transition-colors border-l-4
        ${isActive
          ? 'border-l-amber-400 bg-neutral-100 dark:bg-neutral-800'
          : 'border-l-transparent hover:bg-neutral-50 dark:hover:bg-neutral-900/50'
        }`}
    >
      <div className="flex gap-3 items-start">
        <div className="flex-1 min-w-0">
          <p className={`text-sm font-semibold truncate ${isActive ? 'text-neutral-900 dark:text-white' : 'text-neutral-700 dark:text-neutral-300'}`}>
            <WordRoller text={t(project.title)} />
          </p>
          <p className="text-xs text-neutral-500 dark:text-neutral-400 truncate mt-0.5">
            <WordRoller text={t(project.role)} />, <WordRoller text={t(project.subtitle)} />
          </p>
          <p className="text-xs text-neutral-400 dark:text-neutral-500 truncate mt-0.5">{project.period}</p>
          <div className="flex flex-wrap gap-1 mt-2">
            {project.stack.map(s => <TechBadge key={s} name={s} />)}
          </div>
        </div>
        {thumb && (
          <div className="h-16 w-20 shrink-0 rounded overflow-hidden bg-neutral-100 dark:bg-neutral-800 relative">
            <MediaThumb item={thumb} />
          </div>
        )}
      </div>
    </button>
  )
}

interface Props {
  projects: Project[]
  activeId: string | null
  filterActive: Category | 'all'
  onFilterChange: (c: Category | 'all') => void
  onSelect: (id: string) => void
  onBack: () => void
}

const OVERVIEW: { en: string; ko: string } = { en: 'Overview', ko: '목록' }

export function QuestRail({ projects, activeId, filterActive, onFilterChange, onSelect, onBack }: Props) {
  const { lang } = useLocale()
  return (
    <div className="flex flex-col h-full border-r border-neutral-200/60 dark:border-neutral-800/60 bg-white/50 dark:bg-neutral-950/50">
      <div className="flex items-center gap-2 px-4 py-3 border-b border-neutral-200 dark:border-neutral-800 shrink-0">
        <button
          onClick={onBack}
          className="flex items-center gap-1.5 text-sm text-neutral-600 dark:text-neutral-400 hover:text-neutral-900 dark:hover:text-white transition-colors"
        >
          <svg className="h-4 w-4" viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M7 2L3 6l4 4" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
          <WordRoller text={OVERVIEW[lang]} />
        </button>
      </div>
      <FilterBar active={filterActive} onChange={onFilterChange} compact />
      <div className="flex-1 overflow-y-auto">
        {projects.map(p => (
          <RailItem key={p.id} project={p} isActive={p.id === activeId} onClick={() => onSelect(p.id)} />
        ))}
      </div>
    </div>
  )
}
