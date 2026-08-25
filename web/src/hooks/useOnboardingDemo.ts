import { useEffect, useState } from 'react'
import { useLocale } from './useLocale'
import type { Lang } from '../data/types'

export function useOnboardingDemo() {
  const { lang, setLang } = useLocale()
  const [showTooltip, setShowTooltip] = useState(false)

  useEffect(() => {
    const initial: Lang = (localStorage.getItem('lang') as Lang) || 'en'
    const opposite: Lang = initial === 'en' ? 'ko' : 'en'

    let t2: ReturnType<typeof setTimeout>
    let t3: ReturnType<typeof setTimeout>

    const t1 = setTimeout(() => {
      setLang(opposite)
      t2 = setTimeout(() => {
        setLang(initial)
        t3 = setTimeout(() => {
          setShowTooltip(true)
          setTimeout(() => setShowTooltip(false), 5000)
        }, 400)
      }, 1200)
    }, 1000)

    return () => { clearTimeout(t1); clearTimeout(t2); clearTimeout(t3) }
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  function dismissTooltip() { setShowTooltip(false) }

  return { showTooltip, dismissTooltip }
}
