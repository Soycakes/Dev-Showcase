import type { MediaItem } from '../data/types'

export function MediaSlide({ item }: { item: MediaItem }) {
  if (item.kind === 'image' || item.kind === 'gif') {
    return <img src={item.src} alt={item.alt} className="absolute inset-0 h-full w-full" style={{ objectFit: item.thumbFit ?? 'cover', objectPosition: item.thumbPosition ?? 'center' }} />
  }
  if (item.kind === 'youtube') {
    return (
      <iframe src={`https://www.youtube.com/embed/${item.videoId}`} title={item.title}
        className="absolute inset-0 h-full w-full" allowFullScreen
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" />
    )
  }
  if (item.kind === 'demo') {
    return (
      <a href={item.url} target="_blank" rel="noreferrer"
        className="absolute inset-0 flex items-center justify-center text-sm text-neutral-500 hover:text-neutral-800 dark:hover:text-neutral-200">
        {item.label}
      </a>
    )
  }
  return null
}

export function MediaThumb({ item }: { item: MediaItem }) {
  if (item.kind === 'image' || item.kind === 'gif') {
    return <img src={item.src} alt="" className="h-full w-full" style={{ objectFit: item.thumbFit ?? 'cover', objectPosition: item.thumbPosition ?? 'center' }} />
  }
  if (item.kind === 'youtube') {
    return <img src={`https://img.youtube.com/vi/${item.videoId}/default.jpg`} alt="" className="h-full w-full object-cover" />
  }
  return <div className="h-full w-full bg-neutral-200 dark:bg-neutral-700" />
}
