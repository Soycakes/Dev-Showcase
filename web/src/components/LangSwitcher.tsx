import { useRef, useState, useEffect } from 'react'
import { useLocale } from '../hooks/useLocale'
import { LangTooltip } from './LangTooltip'
import type { Lang } from '../data/types'

const OPTIONS: { lang: Lang; flag: string; label: string }[] = [
  { lang: 'en', flag: '🇺🇸', label: 'English' },
  { lang: 'ko', flag: '🇰🇷', label: '한국어' },
]

interface Props {
  showTooltip: boolean
  onDismissTooltip: () => void
}

export function LangSwitcher({ showTooltip, onDismissTooltip }: Props) {
  const { lang, setLang } = useLocale()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  // Close on outside click
  useEffect(() => {
    if (!open) return
    function handler(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [open])

  const active = OPTIONS.find(o => o.lang === lang)!

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen(o => !o)}
        className="flex items-center gap-2 border border-neutral-200 bg-white px-3 py-1.5 text-sm font-medium text-neutral-700 transition hover:bg-neutral-50 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-200 dark:hover:bg-neutral-900"
        aria-haspopup="listbox"
        aria-expanded={open}
      >
        <span>{active.flag}</span>
        <span>{active.label}</span>
        <svg
          className={`h-3.5 w-3.5 text-neutral-400 transition-transform ${open ? 'rotate-180' : ''}`}
          viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2"
        >
          <path d="M2 4l4 4 4-4" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>

      {showTooltip && <LangTooltip onDismiss={onDismissTooltip} />}

      {open && (
        <ul
          role="listbox"
          className="absolute right-0 mt-1 w-36 border border-neutral-200 bg-white py-1 shadow-lg dark:border-neutral-700 dark:bg-neutral-950"
        >
          {OPTIONS.map(opt => {
            const isActive = opt.lang === lang
            return (
              <li key={opt.lang} role="option" aria-selected={isActive}>
                <button
                  disabled={isActive}
                  onClick={() => { setLang(opt.lang); setOpen(false) }}
                  className={`flex w-full items-center gap-2.5 px-3 py-2 text-sm transition
                    ${isActive
                      ? 'cursor-default text-neutral-400 dark:text-neutral-600'
                      : 'text-neutral-700 hover:bg-neutral-50 dark:text-neutral-200 dark:hover:bg-neutral-800'
                    }`}
                >
                  <span>{opt.flag}</span>
                  <span>{opt.label}</span>
                  {isActive && (
                    <svg className="ml-auto h-3.5 w-3.5" viewBox="0 0 12 12" fill="currentColor">
                      <path d="M2 6l3 3 5-5" stroke="currentColor" strokeWidth="2" fill="none" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                  )}
                </button>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}
