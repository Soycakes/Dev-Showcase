import { useEffect, useState } from 'react'
import { useLocale } from './useLocale'
import type { Lang } from '../data/types'

export function useOnboardingDemo() {
  const { lang, setLang } = useLocale()
  const [showTooltip, setShowTooltip] = useState(false)

  useEffect(() => {
    const initial: Lang = (localStorage.getItem('lang') as Lang) || 'en'
    const opposite: Lang = initial === 'en' ? 'ko' : 'en'

    const t1 = setTimeout(() => {
      setLang(opposite)
      const t2 = setTimeout(() => {
        setLang(initial)
        const t3 = setTimeout(() => {
          setShowTooltip(true)
          setTimeout(() => setShowTooltip(false), 5000)
        }, 400)
        return () => clearTimeout(t3)
      }, 1200)
      return () => clearTimeout(t2)
    }, 1000)

    return () => clearTimeout(t1)
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  function dismissTooltip() { setShowTooltip(false) }

  return { showTooltip, dismissTooltip }
}
