import { useFilter } from './hooks/useFilter'
import { useLocale } from './hooks/useLocale'
import { useOnboardingDemo } from './hooks/useOnboardingDemo'
import { CATEGORIES } from './data/projects'
import { Navbar } from './components/Navbar'
import { SpaceBackground } from './components/SpaceBackground'
import { WordRoller } from './components/WordRoller'
import { TechBadge } from './components/TechIcon'
import type { Category, L10n, Lang } from './data/types'

const HERO_TEXT = {
  name:    { en: 'Hyung Min Park', ko: '박형민' },
  tagline: { en: 'Software Engineer / Game Dev / AI Systems', ko: '소프트웨어 엔지니어 / 게임 개발 / AI 시스템' },
}

function Hero({ demoLang, demoDone }: { demoLang: Lang; demoDone: boolean }) {
  const { lang } = useLocale()
  const activeLang = demoDone ? lang : demoLang

  return (
    <section className="mx-auto max-w-5xl px-6 py-20">
      <h1 className="text-4xl font-bold tracking-tight text-neutral-900 dark:text-white">
        <WordRoller text={HERO_TEXT.name[activeLang]} single />
      </h1>
      <p className="mt-3 text-lg text-neutral-500 dark:text-neutral-400">
        <WordRoller text={HERO_TEXT.tagline[activeLang]} />
      </p>
      <div className="mt-5 flex gap-4">
        <a href="https://github.com/Soycakes" target="_blank" rel="noreferrer"
          className="text-sm text-neutral-500 transition hover:text-neutral-900 dark:hover:text-white">
          GitHub
        </a>
        <a href="https://youtube.com/@Soycake" target="_blank" rel="noreferrer"
          className="text-sm text-neutral-500 transition hover:text-neutral-900 dark:hover:text-white">
          YouTube
        </a>
      </div>
    </section>
  )
}

function FilterBar({ active, onChange }: { active: Category | 'all'; onChange: (c: Category | 'all') => void }) {
  const { t, lang } = useLocale()
  const allLabel: L10n = { en: 'All', ko: '전체' }

  return (
    <div className="mx-auto max-w-5xl px-6">
      <nav className="flex flex-wrap gap-2" aria-label="Project filter">
        {([['all', allLabel]] as [string, L10n][])
          .concat(Object.entries(CATEGORIES) as [Category, L10n][])
          .map(([key, label]) => (
            <button
              key={key}
              onClick={() => onChange(key as Category | 'all')}
              aria-pressed={active === key}
              className={`rounded-full px-4 py-1.5 text-sm font-medium transition
                ${active === key
                  ? 'bg-neutral-900 text-white dark:bg-white dark:text-neutral-900'
                  : 'border border-neutral-200 text-neutral-600 hover:border-neutral-400 dark:border-neutral-700 dark:text-neutral-400 dark:hover:border-neutral-500'
                }`}
            >
              <WordRoller text={t(label)} single />
            </button>
          ))}
      </nav>
    </div>
  )
}

function R({ s, single }: { s: L10n; single?: boolean }) {
  const { t } = useLocale()
  return <WordRoller text={t(s)} single={single} />
}

function ProjectCard({ project }: { project: ReturnType<typeof useFilter>['filtered'][number] }) {
  return (
    <article className="border border-neutral-200 p-5 dark:border-neutral-800" data-id={project.id}>
      <h2 className="font-semibold text-neutral-900 dark:text-white"><R s={project.title} single /></h2>
      <p className="mt-0.5 text-sm text-neutral-500"><R s={project.subtitle} single /> - {project.period}</p>
      <p className="mt-3 text-sm leading-relaxed text-neutral-600 dark:text-neutral-400"><R s={project.summary} /></p>
      {project.bullets.length > 0 && (
        <ul className="mt-3 space-y-1">
          {project.bullets.map((b, i) => (
            <li key={i} className="text-sm text-neutral-600 dark:text-neutral-400 before:mr-2 before:content-['-']">
              <R s={b} />
            </li>
          ))}
        </ul>
      )}
      <div className="mt-4 flex flex-wrap gap-1.5">
        {project.stack.map(s => <TechBadge key={s} name={s} />)}
      </div>
      <div className="mt-4 flex gap-3">
        {project.repoUrl && <a href={project.repoUrl} target="_blank" rel="noreferrer" className="text-xs font-medium text-neutral-500 underline-offset-2 hover:underline">Repo</a>}
        {project.liveUrl && <a href={project.liveUrl} target="_blank" rel="noreferrer" className="text-xs font-medium text-neutral-500 underline-offset-2 hover:underline">Live</a>}
      </div>
    </article>
  )
}

export default function App() {
  const { active, setActive, filtered } = useFilter()
  const { demoLang, done, showTooltip, dismissTooltip } = useOnboardingDemo()

  return (
    <div className="min-h-screen">
      <SpaceBackground />
      <div className="relative z-10">
      <Navbar showTooltip={showTooltip} onDismissTooltip={dismissTooltip} />
      <Hero demoLang={demoLang} demoDone={done} />
      <div id="projects" className="mx-auto max-w-5xl px-6 pb-24">
        {/* <FilterBar active={active} onChange={setActive} /> */}
        <section className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3" aria-label="Projects">
          {filtered.map(p => <ProjectCard key={p.id} project={p} />)}
        </section>
      </div>
      </div>
    </div>
  )
}
