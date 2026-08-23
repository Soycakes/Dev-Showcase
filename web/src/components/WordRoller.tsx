import { useState, useEffect, useRef } from 'react'

// --- Single word slot ---

interface SlotProps { word: string }

function Slot({ word }: SlotProps) {
  const [settled, setSettled] = useState(word)
  const [exiting, setExiting] = useState<string | null>(null)
  const [entering, setEntering] = useState<string | null>(null)

  useEffect(() => {
    if (word === settled) return
    setExiting(settled)
    setEntering(word)
  }, [word]) // eslint-disable-line react-hooks/exhaustive-deps

  function onEnterEnd(e: React.AnimationEvent) {
    if (e.animationName !== 'wordEnter') return
    setSettled(entering!)
    setExiting(null)
    setEntering(null)
  }

  const isAnimating = entering !== null

  if (!isAnimating && settled === '') return null

  return (
    <span className="relative inline-block overflow-hidden" style={{ verticalAlign: 'baseline' }}>
      {/*
        Ghost drives the slot width (invisible, in normal flow).
        Uses ENTERING word so wider incoming text is never clipped mid-roll.
        Width snaps to entering size at animation start — hidden by the roll itself.
      */}
      <span className="invisible select-none whitespace-nowrap" aria-hidden="true">
        {isAnimating ? (entering || ' ') : (settled || ' ')}
      </span>

      {!isAnimating && (
        <span className="absolute inset-0 whitespace-nowrap" aria-hidden="true">
          {settled}
        </span>
      )}

      {isAnimating && (
        <span className="absolute inset-0 whitespace-nowrap animate-word-exit" aria-hidden="true">
          {exiting || ' '}
        </span>
      )}

      {isAnimating && (
        <span
          className="absolute inset-0 whitespace-nowrap animate-word-enter"
          aria-hidden="true"
          onAnimationEnd={onEnterEnd}
        >
          {entering || ' '}
        </span>
      )}
    </span>
  )
}

// --- Word roller ---

const N_BATCHES = 5
const BATCH_INTERVAL_MS = 160

export interface WordRollerProps {
  text: string
  className?: string
  // single=true: treat the whole string as one slot (for names where EN/KO word count differs a lot)
  single?: boolean
}

export function WordRoller({ text, className = '', single = false }: WordRollerProps) {
  const split = (t: string) => (single ? [t] : t.split(' '))

  const [slotWords, setSlotWords] = useState<string[]>(split(text))
  const prevText = useRef(text)
  const slotWordsRef = useRef<string[]>(slotWords)
  const timers = useRef<ReturnType<typeof setTimeout>[]>([])

  useEffect(() => {
    if (text === prevText.current) return
    prevText.current = text

    timers.current.forEach(clearTimeout)
    timers.current = []

    const newWords = split(text)
    const maxLen = Math.max(slotWordsRef.current.length, newWords.length)
    const targets = Array.from({ length: maxLen }, (_, i) => newWords[i] ?? '')

    const changed = targets
      .map((w, i) => (w !== slotWordsRef.current[i] ? i : -1))
      .filter(i => i !== -1)

    const shuffled = [...changed].sort(() => Math.random() - 0.5)
    const batches: number[][] = Array.from({ length: N_BATCHES }, () => [])
    shuffled.forEach((idx, i) => batches[i % N_BATCHES].push(idx))

    slotWordsRef.current = targets

    batches.forEach((batch, bi) => {
      if (batch.length === 0) return
      const t = setTimeout(() => {
        setSlotWords(prev => {
          const next = [...prev]
          while (next.length < targets.length) next.push('')
          batch.forEach(i => { next[i] = targets[i] })
          return next
        })
      }, bi * BATCH_INTERVAL_MS)
      timers.current.push(t)
    })
  }, [text]) // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <span className={`inline-flex flex-wrap gap-x-[0.28em] ${className}`} aria-label={text}>
      {slotWords.map((word, i) => (
        <Slot key={i} word={word} />
      ))}
    </span>
  )
}
