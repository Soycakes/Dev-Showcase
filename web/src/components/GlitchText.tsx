import { useScramble } from '../hooks/useScramble'

interface Props {
  text: string
  className?: string
}

export function GlitchText({ text, className = '' }: Props) {
  const slots = useScramble(text)
  const isAnimating = slots.some(s => s.phase === 'scrambling' || s.phase === 'collapsing')

  return (
    // aria-label gives screen readers the real text; individual chars are hidden
    <span className={`inline-flex ${className}`} aria-label={text}>
      {slots.map(slot => (
        <span
          key={slot.index}
          aria-hidden="true"
          // Fixed width during animation prevents horizontal jitter from mixed glyph widths.
          // Collapsing slots shrink to 0 smoothly (handles EN→KO length mismatch).
          className="inline-block overflow-hidden text-center transition-[width] duration-100"
          style={{
            width: slot.phase === 'collapsing' ? 0
              : isAnimating ? '0.62em'
              : undefined,
          }}
        >
          {/* key on inner span re-mounts it each tick, re-triggering the enter animation */}
          <span
            key={slot.char}
            className={`inline-block ${slot.phase === 'scrambling' ? 'animate-char-enter' : ''}`}
          >
            {slot.char || ' '}
          </span>
        </span>
      ))}
    </span>
  )
}
