import { useState, useMemo } from 'react'
import { createElement } from 'react'
import type { MediaItem } from '../data/types'

export const IMG_SIZE: Record<string, string> = {
  xs: 'max-w-[25%]',
  sm: 'max-w-[40%]',
  lg: 'max-w-[80%]',
  full: 'max-w-full',
}
export const IMG_DEFAULT = 'max-w-[55%]'

type ImageMedia = Extract<MediaItem, { kind: 'image' | 'gif' }>

function imgClass(sizeClass: string, isInline: boolean) {
  const base = 'h-auto rounded-lg border border-neutral-200 dark:border-neutral-800 cursor-zoom-in'
  return isInline
    ? `${sizeClass} ${base} my-1 inline-block align-top mx-1`
    : `${sizeClass} ${base} block mx-auto my-4`
}

export function useMdLightbox(markdown: string, media: MediaItem[] = []) {
  const [lbIndex, setLbIndex] = useState(-1)

  const mediaItems = useMemo(
    () => media.filter((m): m is ImageMedia => m.kind === 'image' || m.kind === 'gif'),
    [media]
  )

  const slides = useMemo(() => {
    const mdImages = [...markdown.matchAll(/!\[([^\]]*)\]\(([^)]+)\)/g)]
    return [
      ...mediaItems.map((m, i) => ({ src: m.src, key: i })),
      ...mdImages.map((m, i) => ({ src: m[2], key: mediaItems.length + i })),
    ]
  }, [markdown, mediaItems])

  const mdComponents = useMemo(() => ({
    img({ src, alt }: { src?: string; alt?: string }) {
      const [label, ...hints] = (alt ?? '').split('|')
      const hintSet = new Set(hints.map(h => h.trim()))
      const sizeHint = [...hintSet].find(h => h in IMG_SIZE && h !== 'inline')
      const sizeClass = (sizeHint && IMG_SIZE[sizeHint]) ?? IMG_DEFAULT
      const isInline = hintSet.has('inline')
      const idx = slides.findIndex(s => s.src === src)
      return createElement('img', {
        src,
        alt: label.trim(),
        loading: 'lazy',
        onClick: () => setLbIndex(idx >= 0 ? idx : 0),
        className: imgClass(sizeClass, isInline),
      })
    },
  }), [slides])

  return { slides, mediaItems, lbIndex, setLbIndex, closeLightbox: () => setLbIndex(-1), mdComponents }
}
