import { WordRoller } from './WordRoller'
import { useLocale } from '../hooks/useLocale'
import { CATEGORIES } from '../data/projects'
import type { Category, L10n } from '../data/types'

interface Props {
  active: Category | 'all'
  onChange: (c: Category | 'all') => void
  compact?: boolean
}

export function FilterBar({ active, onChange, compact }: Props) {
  const { t } = useLocale()
  const allLabel: L10n = { en: 'All', ko: '전체' }

  return (
    <nav
      className={`flex flex-wrap ${compact ? 'gap-1.5 px-3 py-2 border-b border-neutral-100 dark:border-neutral-800' : 'gap-2 mb-6'}`}
      aria-label="Project filter"
    >
      {([['all', allLabel]] as [string, L10n][])
        .concat(Object.entries(CATEGORIES) as [Category, L10n][])
        .map(([key, label]) => (
          <button
            key={key}
            onClick={() => onChange(key as Category | 'all')}
            aria-pressed={active === key}
            className={`shrink-0 rounded-full font-medium transition
              ${compact ? 'px-2.5 py-0.5 text-xs' : 'px-4 py-1.5 text-sm'}
              ${active === key
                ? 'bg-neutral-900 text-white dark:bg-white dark:text-neutral-900'
                : 'border border-neutral-200 text-neutral-600 hover:border-neutral-400 dark:border-neutral-700 dark:text-neutral-400 dark:hover:border-neutral-500'
              }`}
          >
            <WordRoller text={t(label)} single />
          </button>
        ))}
    </nav>
  )
}
