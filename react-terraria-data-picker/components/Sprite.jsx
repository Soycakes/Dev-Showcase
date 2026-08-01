const base = import.meta.env.BASE_URL

export default function Sprite({ id, rawName, name, size = 32 }) {
  return (
    <img
      key={id}
      src={`${base}sprites/Item_${id}.png`}
      alt={name}
      title={name}
      style={{ imageRendering: 'pixelated', width: size, height: size, objectFit: 'contain' }}
      onError={e => {
        const stage = e.target.dataset.stage || '0'
        if (stage === '0') {
          e.target.dataset.stage = '1'
          e.target.src = `https://terraria.wiki.gg/images/${rawName}.png`
        } else if (stage === '1') {
          e.target.dataset.stage = '2'
          e.target.src = `${base}ui/MissingSprite.gif`
        } else {
          e.target.onerror = null
        }
      }}
    />
  )
}
