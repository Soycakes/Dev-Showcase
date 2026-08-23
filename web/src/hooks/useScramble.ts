import { useState, useEffect, useRef } from 'react'

export type SlotPhase = 'idle' | 'scrambling' | 'settled' | 'collapsing'

export interface Slot {
  index: number
  char: string
  phase: SlotPhase
}

interface Options {
  tickMs?: number      // interval between scramble ticks (default 45ms)
  scrambleTicks?: number // how many ticks a slot scrambles before settling (default 4)
  staggerMs?: number   // delay between each slot starting (default 30ms)
}

// Mixed latin+hangul pool — gives the cross-script glitch feel
const POOL =
  'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz' +
  '가나다라마바사아자차카타파하' +
  '각낙닥락막박삭악잭' +
  '고노도로모보소오조초코토포호'

function randomChar() {
  return POOL[Math.floor(Math.random() * POOL.length)]
}

export function useScramble(target: string, options: Options = {}): Slot[] {
  const { tickMs = 45, scrambleTicks = 4, staggerMs = 30 } = options

  const [slots, setSlots] = useState<Slot[]>(() =>
    target.split('').map((char, i) => ({ index: i, char, phase: 'idle' }))
  )

  const prevTarget = useRef(target)
  const animating = useRef(false)

  useEffect(() => {
    if (target === prevTarget.current) return
    const from = prevTarget.current
    prevTarget.current = target

    if (animating.current) return  // let current animation finish
    animating.current = true

    const maxLen = Math.max(from.length, target.length)

    // Build initial slot state — always maxLen slots
    const initialSlots: Slot[] = Array.from({ length: maxLen }, (_, i) => ({
      index: i,
      char: i < from.length ? from[i] : '',
      phase: 'idle',
    }))
    setSlots(initialSlots)

    // Per-slot scramble progress counter
    const progress = new Array(maxLen).fill(0)
    const started = new Array(maxLen).fill(false)

    // Stagger slot start times
    const startTimers = initialSlots.map((_, i) =>
      setTimeout(() => { started[i] = true }, i * staggerMs)
    )

    const interval = setInterval(() => {
      setSlots(prev => {
        const next = prev.map((slot, i) => {
          if (!started[i]) return slot
          if (slot.phase === 'settled' || slot.phase === 'collapsing') return slot

          progress[i]++

          // Still scrambling
          if (progress[i] <= scrambleTicks) {
            return { ...slot, char: randomChar(), phase: 'scrambling' as SlotPhase }
          }

          // Settle to target
          const targetChar = i < target.length ? target[i] : ''
          return {
            ...slot,
            char: targetChar,
            phase: (targetChar === '' ? 'collapsing' : 'settled') as SlotPhase,
          }
        })

        // Done when all started slots have settled
        const allDone = next.every((s, i) => !started[i] || s.phase === 'settled' || s.phase === 'collapsing')
        if (allDone) {
          clearInterval(interval)
          animating.current = false
          // Trim collapsed slots
          return next.filter(s => s.phase !== 'collapsing')
        }

        return next
      })
    }, tickMs)

    return () => {
      startTimers.forEach(clearTimeout)
      clearInterval(interval)
      animating.current = false
    }
  }, [target, tickMs, scrambleTicks, staggerMs])

  return slots
}
