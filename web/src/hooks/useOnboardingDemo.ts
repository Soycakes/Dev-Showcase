import { useEffect, useState } from 'react'
import type { Lang } from '../data/types'

export function useOnboardingDemo() {
  const initial: Lang = (localStorage.getItem('lang') as Lang) || 'en'
  const opposite: Lang = initial === 'en' ? 'ko' : 'en'
  const [demoLang, setDemoLang] = useState<Lang>(initial)
  const [done, setDone] = useState(false)
  const [showTooltip, setShowTooltip] = useState(false)

  useEffect(() => {
    const t1 = setTimeout(() => {
      setDemoLang(opposite)
      const t2 = setTimeout(() => {
        setDemoLang(initial)
        const t3 = setTimeout(() => {
          setDone(true)
          setShowTooltip(true)
          setTimeout(() => setShowTooltip(false), 5000)
        }, 400)
        return () => clearTimeout(t3)
      }, 1200)
      return () => clearTimeout(t2)
    }, 1000)

    return () => clearTimeout(t1)
  }, [])

  function dismissTooltip() { setShowTooltip(false) }

  return { demoLang, done, showTooltip, dismissTooltip }
}
