import { TECH_ICONS } from '../data/icons'

interface Props {
  name: string
  size?: number
  color?: 'brand' | 'current'
  className?: string
}

export function TechIcon({ name, size = 14, color = 'current', className = '' }: Props) {
  const icon = TECH_ICONS[name]
  if (!icon) return null

  const fill = color === 'brand' ? `#${icon.hex}` : 'currentColor'

  return (
    <svg
      role="img"
      viewBox="0 0 24 24"
      width={size}
      height={size}
      fill={fill}
      aria-label={icon.title}
      className={className}
    >
      <path d={icon.path} />
    </svg>
  )
}

export function TechBadge({ name }: { name: string }) {
  const hasIcon = Boolean(TECH_ICONS[name])
  return (
    <span className="inline-flex items-center gap-1 rounded-md bg-neutral-100 px-2 py-0.5 text-xs text-neutral-600 dark:bg-neutral-800 dark:text-neutral-400">
      {hasIcon && <TechIcon name={name} size={11} color="brand" />}
      {name}
    </span>
  )
}
