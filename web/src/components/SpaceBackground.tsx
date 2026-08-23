import { useEffect, useRef } from 'react'
import { CFG, STAR_CHARS, BIG_STAR_CHARS, MORPH_SEQS, PLANETS, ROCKET_LINES, FLAME_FRAMES } from './spaceConfig'

function rnd(a: number, b: number) { return a + Math.random() * (b - a) }
function pick<T>(arr: T[]): T { return arr[Math.floor(Math.random() * arr.length)] }

type Star = {
  x: number
  y: number
  char: string
  morph: string[] | null
  morphOffset: number
  phase: number
  speed: number
  opacity: number
  scale: number
}
type Planet = { x: number; y: number; lines: string[]; opacity: number }

function buildScene(w: number, h: number): { stars: Star[]; planets: Planet[] } {
  const r = Math.sqrt(w * w + h * h) / 2

  const stars: Star[] = Array.from({ length: CFG.totalStars }, (_, i) => {
    const dist = Math.sqrt(Math.random()) * r
    const theta = Math.random() * Math.PI * 2
    const isMorphing = i < CFG.morphingCount
    const isBig = i >= CFG.morphingCount && i < CFG.morphingCount + CFG.bigStarCount
    return {
      x: Math.cos(theta) * dist,
      y: Math.sin(theta) * dist,
      char: isBig ? pick(BIG_STAR_CHARS) : pick(STAR_CHARS),
      morph: isMorphing ? pick(MORPH_SEQS) : null,
      morphOffset: Math.random() * (CFG.morphStepMs * 10),
      phase: Math.random() * Math.PI * 2,
      speed: rnd(...CFG.starSpeed),
      opacity: isBig ? rnd(...CFG.bigStarOpacity) : rnd(...CFG.starOpacity),
      scale: isBig ? rnd(...CFG.bigStarScale) : 1,
    }
  })

  const planets: Planet[] = PLANETS.map((lines, i) => {
    const theta = (i / PLANETS.length) * Math.PI * 2 + 0.5
    const dist = r * rnd(...CFG.planetDistRatio)
    return { x: Math.cos(theta) * dist, y: Math.sin(theta) * dist, lines, opacity: rnd(...CFG.planetOpacity) }
  })

  return { stars, planets }
}

export function SpaceBackground() {
  const canvasRef = useRef<HTMLCanvasElement>(null)

  useEffect(() => {
    const canvas = canvasRef.current!
    const ctx = canvas.getContext('2d')!
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches

    let scene = buildScene(window.innerWidth, window.innerHeight)

    let mouseX = -9999
    let mouseY = -9999
    let rocketX = -9999
    let rocketY = -9999
    let rocketAngle = 0
    let hasMouse = false

    function onMouseMove(e: MouseEvent) {
      mouseX = e.clientX
      mouseY = e.clientY
      if (!hasMouse) {
        rocketX = mouseX
        rocketY = mouseY
        hasMouse = true
      }
    }

    window.addEventListener('mousemove', onMouseMove)

    function resize() {
      const dpr = window.devicePixelRatio || 1
      const w = window.innerWidth
      const h = window.innerHeight
      canvas.width = w * dpr
      canvas.height = h * dpr
      canvas.style.width = w + 'px'
      canvas.style.height = h + 'px'
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0)
      scene = buildScene(w, h)
    }

    resize()

    function isDark() {
      const t = document.documentElement.getAttribute('data-theme')
      if (t === 'dark') return true
      if (t === 'light') return false
      return window.matchMedia('(prefers-color-scheme: dark)').matches
    }

    let angle = 0
    let lastT = 0
    let rafId: number

    function loop(t: number) {
      const dt = Math.min(t - lastT, CFG.dtCap)
      lastT = t

      if (!document.hidden) {
        const w = window.innerWidth
        const h = window.innerHeight
        const cx = w / 2
        const cy = h / 2
        const dark = isDark()
        const { colors } = CFG

        if (!reducedMotion) angle += CFG.rotationPerMs * dt

        if (hasMouse) {
          rocketX += (mouseX - rocketX) * CFG.rocketLerp
          rocketY += (mouseY - rocketY) * CFG.rocketLerp
          const dx = mouseX - rocketX
          const targetAngle = Math.max(-1, Math.min(1, dx / CFG.rocketTiltDistance)) * CFG.rocketMaxTiltDeg
          rocketAngle += (targetAngle - rocketAngle) * CFG.rocketLerp
        }

        ctx.clearRect(0, 0, w, h)

        ctx.save()
        ctx.translate(cx, cy)
        if (!reducedMotion) ctx.rotate(angle)
        ctx.translate(-cx, -cy)
        ctx.textBaseline = 'middle'
        ctx.textAlign = 'center'

        for (const s of scene.stars) {
          const tw = reducedMotion
            ? s.opacity
            : s.opacity * (CFG.twinkleDim + CFG.twinkleSwing * Math.sin(t * 0.001 * s.speed + s.phase))
          ctx.globalAlpha = dark ? tw : tw * CFG.lightModeStarAlpha
          ctx.fillStyle = dark ? colors.starDark : colors.starLight

          const char = (!reducedMotion && s.morph)
            ? s.morph[Math.floor((t + s.morphOffset) / CFG.morphStepMs) % s.morph.length]
            : s.char

          ctx.font = `${CFG.fontSize * s.scale}px "Space Mono", monospace`
          ctx.fillText(char, cx + s.x, cy + s.y)
        }

        ctx.font = `${CFG.fontSize}px "Space Mono", monospace`
        ctx.textAlign = 'left'
        for (const p of scene.planets) {
          ctx.globalAlpha = dark ? p.opacity : p.opacity * CFG.lightModePlanetAlpha
          ctx.fillStyle = dark ? colors.planetDark : colors.planetLight
          p.lines.forEach((line, i) => {
            ctx.fillText(line, cx + p.x, cy + p.y + i * CFG.fontSize * CFG.planetLineHeight)
          })
        }

        ctx.restore()

        if (hasMouse) {
          const rocketFontSize = CFG.fontSize * CFG.rocketScale
          ctx.font = `${rocketFontSize}px "Space Mono", monospace`
          ctx.textBaseline = 'middle'
          ctx.textAlign = 'left'

          const charW = ctx.measureText('M').width
          const lineH = rocketFontSize * CFG.planetLineHeight
          const maxLen = Math.max(...ROCKET_LINES.map(l => l.length))
          const blockW = maxLen * charW
          const totalLines = ROCKET_LINES.length + FLAME_FRAMES[0].length
          const rx = rocketX - blockW / 2
          const ry = rocketY - lineH

          const pivotX = rx + blockW / 2
          const pivotY = ry + (ROCKET_LINES.length / 2) * lineH
          ctx.save()
          ctx.translate(pivotX, pivotY)
          ctx.rotate(rocketAngle * Math.PI / 180)
          ctx.translate(-pivotX, -pivotY)

          ctx.globalAlpha = 1
          ctx.fillStyle = dark ? CFG.bgDark : CFG.bgLight
          ctx.fillRect(rx - 2, ry - lineH, blockW + 4, (totalLines + 1) * lineH)

          const flameFrame = FLAME_FRAMES[Math.floor(t / CFG.flameStepMs) % FLAME_FRAMES.length]
          ctx.globalAlpha = dark ? CFG.flameOpacity : CFG.flameOpacity * 0.5
          ctx.fillStyle = dark ? colors.flameDark : colors.flameLight
          ctx.font = `${rocketFontSize}px "Space Mono", monospace`
          flameFrame.forEach((line, i) => {
            ctx.fillText(line, rx, ry + (ROCKET_LINES.length + i) * lineH)
          })

          ctx.globalAlpha = dark ? CFG.rocketOpacity : CFG.rocketOpacity * 0.6
          ctx.fillStyle = dark ? colors.starDark : colors.starLight
          ctx.font = `${rocketFontSize}px "Space Mono", monospace`
          ROCKET_LINES.forEach((line, i) => {
            ctx.fillText(line, rx, ry + i * lineH)
          })

          ctx.restore()
        }

        ctx.globalAlpha = 1
      }

      rafId = requestAnimationFrame(loop)
    }

    rafId = requestAnimationFrame(loop)
    window.addEventListener('resize', resize)
    return () => {
      cancelAnimationFrame(rafId)
      window.removeEventListener('resize', resize)
      window.removeEventListener('mousemove', onMouseMove)
    }
  }, [])

  return <canvas ref={canvasRef} className="pointer-events-none fixed inset-0 -z-10" aria-hidden="true" />
}
