import { LangSwitcher } from './LangSwitcher'
import { useLocale } from '../hooks/useLocale'
import { WordRoller } from './WordRoller'

const NAV_LINKS = [
  { href: '#contact', en: 'Contact', ko: '연락하기' },
]

interface NavbarProps {
  showTooltip: boolean
  onDismissTooltip: () => void
  onLogoClick?: () => void
}

export function Navbar({ showTooltip, onDismissTooltip, onLogoClick }: NavbarProps) {
  const { lang } = useLocale()

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
              <li key={link.href}>
                <a href={link.href} className="text-sm text-neutral-500 transition hover:text-neutral-900 dark:text-neutral-400 dark:hover:text-white">
                  <WordRoller text={lang === 'en' ? link.en : link.ko} single />
                </a>
              </li>
            ))}
          </ul>
          <LangSwitcher showTooltip={showTooltip} onDismissTooltip={onDismissTooltip} />
        </div>
      </nav>
    </header>
  )
}
