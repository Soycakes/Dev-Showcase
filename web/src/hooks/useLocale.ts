import { useLang } from '../context/LangContext'
import type { L10n } from '../data/types'

export function useLocale() {
  const { lang, setLang } = useLang()
  const t = (s: L10n) => s[lang]
  return { lang, setLang, t }
}
