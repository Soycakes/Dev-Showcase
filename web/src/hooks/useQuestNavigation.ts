import { useEffect } from 'react'
import type { Project } from '../data/types'

interface Params {
  filtered: Project[]
  activeId: string | null
  setActiveId: (id: string) => void
  exitQuest: () => void
}

export function useQuestNavigation({ filtered, activeId, setActiveId, exitQuest }: Params) {
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (!activeId || filtered.length === 0) return
      const idx = filtered.findIndex(p => p.id === activeId)
      if (e.key === 'ArrowDown') {
        e.preventDefault()
        setActiveId(filtered[(idx + 1) % filtered.length].id)
      }
      if (e.key === 'ArrowUp') {
        e.preventDefault()
        setActiveId(filtered[(idx - 1 + filtered.length) % filtered.length].id)
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [activeId, filtered, exitQuest, setActiveId])

  useEffect(() => {
    if (!activeId) return
    if (new URLSearchParams(window.location.search).get('project') === activeId) return
    const url = new URL(window.location.href)
    url.searchParams.set('project', activeId)
    window.history.pushState({ project: activeId }, '', url)
  }, [activeId])

  useEffect(() => {
    function onPop() {
      const id = new URLSearchParams(window.location.search).get('project')
      if (!id) exitQuest()
      else setActiveId(id)
    }
    window.addEventListener('popstate', onPop)
    return () => window.removeEventListener('popstate', onPop)
  }, [exitQuest, setActiveId])
}
