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
      className={`flex flex-wrap ${compact ? 'gap-x-3 gap-y-1 px-4 py-2 border-b border-neutral-200 dark:border-neutral-800' : 'gap-x-5 gap-y-2 mb-2'}`}
      aria-label="Project filter"
    >
      {([['all', allLabel]] as [string, L10n][])
        .concat(Object.entries(CATEGORIES) as [Category, L10n][])
        .map(([key, label]) => (
          <button
            key={key}
            onClick={() => onChange(key as Category | 'all')}
            aria-pressed={active === key}
            className={`shrink-0 transition-colors
              ${compact ? 'text-xs' : 'text-sm'}
              ${active === key
                ? 'font-semibold text-neutral-900 dark:text-white'
                : 'text-neutral-400 hover:text-neutral-700 dark:text-neutral-500 dark:hover:text-neutral-300'
              }`}
          >
            <WordRoller text={t(label)} />
          </button>
        ))}
    </nav>
  )
}
