import { LangSwitcher } from './LangSwitcher'
import { useLocale } from '../hooks/useLocale'
import { WordRoller } from './WordRoller'
import { useState } from 'react'

const NAV_LINKS = [
  { href: 'https://github.com/Soycakes/Dev-Showcase', en: 'Github', ko: 'Github', external: true },
  { href: null, en: 'Contact', ko: '이메일' },
]

interface NavbarProps {
  showTooltip: boolean
  onDismissTooltip: () => void
  onLogoClick?: () => void
}

export function Navbar({ showTooltip, onDismissTooltip, onLogoClick }: NavbarProps) {
  const { lang } = useLocale()
  const [copied, setCopied] = useState(false)

  function handleContact() {
    navigator.clipboard.writeText('luke123park321' + '@' + 'gmail.com')
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <header className="sticky top-0 z-40 w-full border-b border-neutral-200 bg-white/80 backdrop-blur-sm dark:border-neutral-800 dark:bg-neutral-950/80">
      <nav className="mx-auto flex max-w-5xl items-center justify-between px-6 py-3">
        <a
          href="#"
          onClick={onLogoClick ? (e) => { e.preventDefault(); onLogoClick() } : undefined}
          className="text-sm font-semibold tracking-tight text-neutral-900 dark:text-white"
        >
          Luke Park
        </a>
        <div className="flex items-center gap-6">
          <ul className="hidden items-center gap-5 sm:flex">
            {NAV_LINKS.map(link => (
              <li key={link.en}>
                {link.href === null
                  ? <button onClick={handleContact} className="text-sm text-neutral-500 transition hover:text-neutral-900 dark:text-neutral-400 dark:hover:text-white cursor-pointer">
                      {copied ? (lang === 'en' ? 'Copied!' : '복사됨!') : <WordRoller text={lang === 'en' ? link.en : link.ko} single />}
                    </button>
                  : <a href={link.href} {...(link.external ? { target: '_blank', rel: 'noreferrer' } : {})} className="text-sm text-neutral-500 transition hover:text-neutral-900 dark:text-neutral-400 dark:hover:text-white">
                      <WordRoller text={lang === 'en' ? link.en : link.ko} single />
                    </a>
                }
              </li>
            ))}
          </ul>
          <LangSwitcher showTooltip={showTooltip} onDismissTooltip={onDismissTooltip} />
        </div>
      </nav>
    </header>
  )
}
