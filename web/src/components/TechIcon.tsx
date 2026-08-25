import { TECH_ICONS, CUSTOM_ICONS } from '../data/icons'

interface Props {
  name: string
  size?: number
  color?: 'brand' | 'current'
  className?: string
}

function isDarkColor(hex: string): boolean {
  const rgb = parseInt(hex, 16)
  if (isNaN(rgb)) return false
  const r = (rgb >> 16) & 0xff
  const g = (rgb >> 8) & 0xff
  const b = rgb & 0xff
  return (0.299 * r + 0.587 * g + 0.114 * b) < 55
}

export function TechIcon({ name, size = 14, color = 'current', className = '' }: Props) {
  const customUrl = CUSTOM_ICONS[name]
  if (customUrl) {
    return <img src={customUrl} width={size} height={size} alt={name} className={className} />
  }

  const icon = TECH_ICONS[name]
  if (!icon) return null

  const fill = color === 'brand' ? `#${icon.hex}` : 'currentColor'
  const isDarkBrand = color === 'brand' && isDarkColor(icon.hex)
  const fillClass = isDarkBrand ? 'dark:!fill-white' : ''

  return (
    <svg
      role="img"
      viewBox="0 0 24 24"
      width={size}
      height={size}
      fill={fill}
      aria-label={icon.title}
      className={`${className} ${fillClass}`}
    >
      <path d={icon.path} />
    </svg>
  )
}

export function TechBadge({ name }: { name: string }) {
  const hasIcon = Boolean(TECH_ICONS[name] ?? CUSTOM_ICONS[name])
  return (
    <span className="inline-flex items-center gap-1 rounded-md bg-neutral-100 px-2 py-0.5 text-xs text-neutral-600 dark:bg-neutral-800 dark:text-neutral-400">
      {hasIcon && <TechIcon name={name} size={11} color="brand" />}
      {name}
    </span>
  )
}
