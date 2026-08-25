import { useEffect, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { useFilter } from './hooks/useFilter'
import { useLocale } from './hooks/useLocale'
import { useOnboardingDemo } from './hooks/useOnboardingDemo'
import { useQuestNavigation } from './hooks/useQuestNavigation'
import { Navbar } from './components/Navbar'
import { SpaceBackground } from './components/SpaceBackground'
import { ProjectCard } from './components/ProjectCard'
import { FilterBar } from './components/FilterBar'
import { QuestRail } from './components/QuestRail'
import { QuestDetail } from './components/QuestDetail'
import { WordRoller } from './components/WordRoller'
import { TechIcon } from './components/TechIcon'
const HERO_TEXT = {
  name: { en: 'Hyung Min Park', ko: '박형민' },
  tagline: { en: 'Software Engineer / Game Dev / AI Systems', ko: '소프트웨어 엔지니어 / 게임 개발 / AI 시스템' },
}

function Hero() {
  const { lang } = useLocale()

  return (
    <section className="mx-auto max-w-5xl px-6 py-20">
      <h1 className="text-4xl font-bold tracking-tight text-neutral-900 dark:text-white">
        <WordRoller text={HERO_TEXT.name[lang]} single />
      </h1>
      <p className="mt-3 text-lg text-neutral-400 dark:text-neutral-300">
        <WordRoller text={HERO_TEXT.tagline[lang]} />
      </p>
      <div className="mt-5 flex gap-4">
        <a href="https://github.com/Soycakes" target="_blank" rel="noreferrer"
          className="flex items-center gap-1.5 text-sm text-neutral-400 transition hover:text-neutral-900 dark:hover:text-white">
          <TechIcon name="GitHub" size={16} color="brand" />
          GitHub
        </a>
        <a href="https://youtube.com/@Soycake" target="_blank" rel="noreferrer"
          className="flex items-center gap-1.5 text-sm text-neutral-400 transition hover:text-neutral-900 dark:hover:text-white">
          <TechIcon name="YouTube" size={16} color="brand" />
          YouTube
        </a>
      </div>
    </section>
  )
}

export default function App() {
  const { active, setActive, filtered } = useFilter()
  const { showTooltip, dismissTooltip } = useOnboardingDemo()
  const [mode, setMode] = useState<'scroll' | 'quest'>('scroll')
  const [activeId, setActiveId] = useState<string | null>(null)
  const [scrollToCode, setScrollToCode] = useState(false)

  useEffect(() => {
    const id = new URLSearchParams(window.location.search).get('project')
    if (id) { setActiveId(id); setMode('quest') }
  }, [])

  useEffect(() => {
    if (mode !== 'quest' || !activeId) return
    if (!filtered.some(p => p.id === activeId) && filtered.length > 0) {
      setActiveId(filtered[0].id)
    }
  }, [filtered, mode, activeId])

  function enterQuest(id: string) {
    setActiveId(id)
    setMode('quest')
  }

  function enterQuestAtCode(id: string) {
    setActiveId(id)
    setMode('quest')
    setScrollToCode(true)
  }

  function exitQuest() {
    setMode('scroll')
    setActiveId(null)
    const url = new URL(window.location.href)
    url.searchParams.delete('project')
    window.history.pushState({}, '', url)
  }

  const activeIdx = filtered.findIndex(p => p.id === activeId)
  const activeProject = filtered[activeIdx] ?? filtered[0] ?? null
  const hasMultiple = filtered.length > 1
  const nextProject = hasMultiple ? filtered[(activeIdx + 1) % filtered.length] : null
  const prevProject = hasMultiple ? filtered[(activeIdx - 1 + filtered.length) % filtered.length] : null

  useQuestNavigation({ filtered, activeId, setActiveId, exitQuest })

  const isQuest = mode === 'quest'

  return (
    <div className={isQuest ? 'h-screen overflow-hidden flex flex-col' : 'min-h-screen'}>
      <SpaceBackground />
      <div className={`relative z-10 ${isQuest ? 'flex flex-col flex-1 overflow-hidden' : ''}`}>
        <Navbar
          showTooltip={showTooltip}
          onDismissTooltip={dismissTooltip}
          onLogoClick={isQuest ? exitQuest : undefined}
        />

        <AnimatePresence mode="wait">
          {!isQuest ? (
            <motion.div
              key="scroll"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0, x: -20 }}
              transition={{ duration: 0.2 }}
            >
              <Hero />
              <div id="projects" className="mx-auto max-w-5xl px-6 pb-24">
                <FilterBar active={active} onChange={setActive} />
                <section className="space-y-4" aria-label="Projects">
                  {filtered.map(p => (
                    <div key={p.id} className="cursor-pointer" onClick={() => enterQuest(p.id)}>
                      <ProjectCard project={p} onCodeClick={() => enterQuestAtCode(p.id)} />
                    </div>
                  ))}
                </section>
              </div>
            </motion.div>
          ) : (
            <motion.div
              key="quest"
              className="flex flex-1 overflow-hidden justify-center"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.2 }}
            >
              <div className="flex w-full max-w-[1280px] h-full overflow-hidden">
              <div className="hidden lg:flex lg:flex-col w-[320px] shrink-0 h-full">
                <QuestRail
                  projects={filtered}
                  activeId={activeId}
                  filterActive={active}
                  onFilterChange={setActive}
                  onSelect={setActiveId}
                  onBack={exitQuest}
                />
              </div>
              <div className="flex-1 h-full overflow-hidden">
                <AnimatePresence mode="wait">
                  {activeProject && (
                    <motion.div
                      key={activeProject.id}
                      className="h-full"
                      initial={{ opacity: 0, x: 20 }}
                      animate={{ opacity: 1, x: 0 }}
                      exit={{ opacity: 0 }}
                      transition={{ duration: 0.18, ease: 'easeOut' }}
                    >
                      <QuestDetail
                        project={activeProject}
                        prevProject={prevProject}
                        nextProject={nextProject}
                        onPrev={() => prevProject && setActiveId(prevProject.id)}
                        onNext={() => nextProject && setActiveId(nextProject.id)}
                        onBack={exitQuest}
                        scrollToCode={scrollToCode}
                        onScrollCodeDone={() => setScrollToCode(false)}
                      />
                    </motion.div>
                  )}
                </AnimatePresence>
              </div>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </div>
  )
}
