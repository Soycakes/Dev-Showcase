import { useEffect, useRef } from 'react'

const reducedMotion = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches

export function MdVideo({ src, className }: { src: string; className: string }) {
  const ref = useRef<HTMLVideoElement>(null)
  const still = reducedMotion()

  useEffect(() => {
    const video = ref.current
    if (!video || still) return
    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) video.play().catch(() => {})
      else video.pause()
    })
    observer.observe(video)
    return () => observer.disconnect()
  }, [still])

  return <video ref={ref} src={src} className={className} muted loop playsInline controls={still} preload="metadata" />
}
