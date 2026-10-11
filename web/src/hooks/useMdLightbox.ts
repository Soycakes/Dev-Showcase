import { useState, useMemo, createElement } from 'react'
import type { ReactNode } from 'react'
import { MdVideo } from '../components/MdVideo'
import type { MediaItem } from '../data/types'

export const IMG_SIZE: Record<string, string> = {
  xs: 'max-w-[25%]',
  sm: 'max-w-[40%]',
  lg: 'max-w-[80%]',
  full: 'max-w-full',
}
export const IMG_DEFAULT = 'max-w-[55%]'

type ImageMedia = Extract<MediaItem, { kind: 'image' | 'gif' }>

const isVideo = (src?: string) => /\.(mp4|webm)$/i.test(src ?? '')

function parseAlt(alt?: string) {
  const [label, ...hints] = (alt ?? '').split('|')
  return { label: label.trim(), hints: new Set(hints.map(h => h.trim())) }
}

function imgClass(sizeClass: string, isInline: boolean) {
  const base = 'h-auto rounded-lg border border-neutral-200 dark:border-neutral-800 cursor-zoom-in'
  return isInline
    ? `${sizeClass} ${base} my-1 inline-block align-top mx-1`
    : `${sizeClass} ${base} block mx-auto my-4`
}

const GALLERY = 'grid gap-3 my-4 [&_figure]:m-0 [&_img]:!max-w-full [&_img]:!w-full [&_img]:!my-0 [&_video]:!max-w-full [&_video]:!w-full [&_video]:!my-0'
const galleryCols = (n: number) => n === 2 ? 'grid-cols-2' : n === 4 ? 'grid-cols-2 md:grid-cols-4' : 'grid-cols-2 md:grid-cols-3'
const FIGURE = 'my-4 [&_img]:!my-0 [&_video]:!my-0'

type HastNode = { type: string; tagName?: string; value?: string; properties?: { alt?: string; src?: string } }

export function useMdLightbox(markdown: string, media: MediaItem[] = []) {
  const [lbIndex, setLbIndex] = useState(-1)

  const mediaItems = useMemo(
    () => media.filter((m): m is ImageMedia => m.kind === 'image' || m.kind === 'gif'),
    [media]
  )

  const slides = useMemo(() => {
    const mdImages = [...markdown.matchAll(/!\[([^\]]*)\]\(([^)]+)\)/g)].filter(m => !isVideo(m[2]))
    return [
      ...mediaItems.map((m, i) => ({ src: m.src, key: i })),
      ...mdImages.map((m, i) => ({ src: m[2], key: mediaItems.length + i, intro: parseAlt(m[1]).label || undefined })),
    ]
  }, [markdown, mediaItems])

  const mdComponents = useMemo(() => ({
    img({ src, alt }: { src?: string; alt?: string }) {
      const { label, hints } = parseAlt(alt)
      const sizeHint = [...hints].find(h => h in IMG_SIZE)
      const sizeClass = (sizeHint && IMG_SIZE[sizeHint]) ?? IMG_DEFAULT
      const isInline = hints.has('inline')
      const idx = slides.findIndex(s => s.src === src)
      const el = isVideo(src)
        ? createElement(MdVideo, { src: src!, className: imgClass(sizeClass, isInline).replace(' cursor-zoom-in', '') })
        : createElement('img', {
            src,
            alt: label,
            loading: 'lazy',
            onClick: () => setLbIndex(idx >= 0 ? idx : 0),
            className: imgClass(sizeClass, isInline),
          })
      if (!label) return el
      return createElement('figure', { className: 'm-0' }, el,
        createElement('figcaption', { className: 'mt-1 text-center text-xs text-neutral-500 dark:text-neutral-400' }, label))
    },
    p({ node, children }: { node?: { children: HastNode[] }; children?: ReactNode }) {
      const items = (node?.children ?? []).filter(c => !(c.type === 'text' && !c.value?.trim()))
      const allMedia = items.length > 0 && items.every(c => c.tagName === 'img')
      const alts = items.map(c => parseAlt(c.properties?.alt))
      const needsBlock = items.some((c, i) => alts[i].label || isVideo(c.properties?.src))
      if (!allMedia || alts.some(a => a.hints.has('inline')) || (items.length < 2 && !needsBlock)) {
        return createElement('p', null, children)
      }
      return createElement('div', { className: items.length > 1 ? `${GALLERY} ${galleryCols(items.length)}` : FIGURE }, children)
    },
  }), [slides])

  return { slides, mediaItems, lbIndex, setLbIndex, closeLightbox: () => setLbIndex(-1), mdComponents }
}
