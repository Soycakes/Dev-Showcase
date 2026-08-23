import { useState, useMemo } from 'react'
import { projects } from '../data/projects'
import type { Category } from '../data/types'

export function useFilter() {
  const [active, setActive] = useState<Category | 'all'>('all')

  const filtered = useMemo(
    () => active === 'all' ? projects : projects.filter(p => p.categories.includes(active)),
    [active]
  )

  return { active, setActive, filtered }
}
