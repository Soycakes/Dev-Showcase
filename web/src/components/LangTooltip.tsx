interface Props {
  onDismiss: () => void
}

export function LangTooltip({ onDismiss }: Props) {
  return (
    <div
      className="animate-fade-in absolute right-0 top-full z-50 mt-2 cursor-pointer select-none"
      onClick={onDismiss}
    >
      <div className="absolute -top-1.5 right-4 h-3 w-3 rotate-45 bg-neutral-800 dark:bg-white" />
      <div className="bg-neutral-800 px-3 py-2 text-xs whitespace-nowrap text-white shadow-lg dark:bg-white dark:text-neutral-900">
        Switch language here!
      </div>
    </div>
  )
}
