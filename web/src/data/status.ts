import type { Project } from './types'

export const STATUS_STYLE: Record<Project['status'], string> = {
  shipped: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-400',
  wip: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-400',
  archived: 'bg-neutral-100 text-neutral-500 dark:bg-neutral-800 dark:text-neutral-400',
}

export const STATUS_LABEL = {
  en: { shipped: 'Shipped', wip: 'In Progress', archived: 'Archived' },
  ko: { shipped: '출시됨', wip: '진행중', archived: '아카이브' },
}
