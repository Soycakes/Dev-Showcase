import { createContext, useContext, useState, type ReactNode } from 'react'
import type { Lang } from '../data/types'

const STORAGE_KEY = 'lang'

function getSavedLang(): Lang {
  const v = localStorage.getItem(STORAGE_KEY)
  return v === 'ko' ? 'ko' : 'en'
}

interface LangCtx {
  lang: Lang
  setLang: (l: Lang) => void
}

const LangContext = createContext<LangCtx | null>(null)

export function LangProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<Lang>(getSavedLang)

  function setLang(l: Lang) {
    localStorage.setItem(STORAGE_KEY, l)
    setLangState(l)
  }

  return <LangContext.Provider value={{ lang, setLang }}>{children}</LangContext.Provider>
}

export function useLang(): LangCtx {
  const ctx = useContext(LangContext)
  if (!ctx) throw new Error('useLang must be inside LangProvider')
  return ctx
}
