import { useState, useEffect } from 'react'
import Sprite from './Sprite'

function matches(weapon, field, value) {
  const val = weapon.data[field]
  return Array.isArray(val) ? val.includes(value) : val === value
}

export default function CellModal({ weapons, rowAxis, colAxis, row, col, onPick, onClose }) {
  const [search, setSearch] = useState('')

  useEffect(() => {
    const onKey = e => { if (e.key === 'Escape') onClose() }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  const valid = weapons.filter(w =>
    matches(w, rowAxis.field, row) &&
    matches(w, colAxis.field, col) &&
    w.data.name.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div onClick={onClose} style={overlay}>
      <div onClick={e => e.stopPropagation()} style={box}>
        <p style={{ marginBottom: '0.5rem' }}>{rowAxis?.rename?.[row] || row} / {colAxis?.rename?.[col] || col}</p>
        <input
          autoFocus
          placeholder="Search..."
          value={search}
          onChange={e => setSearch(e.target.value)}
          style={{ width: '100%', marginBottom: '0.5rem', padding: '4px' }}
        />
        <div style={list}>
          <button onClick={() => onPick(null)} style={clearBtn}>Clear</button>
          {valid.map(w => (
            <button key={w.rawName} onClick={() => onPick(w)} style={itemBtn}>
              <Sprite id={w.data.id} rawName={w.rawName} name={w.data.name} />
              <span>{w.data.name}</span>
            </button>
          ))}
          {valid.length === 0 && (
            <div style={{ textAlign: 'center', padding: '1rem' }}>
              <img src={`${import.meta.env.BASE_URL}ui/MissingSprite.gif`} style={{ imageRendering: 'pixelated', width: 96, height: 96, display: 'block', margin: '0 auto 0.5rem' }} />
              No weapons found.
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

const overlay = { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 10 }
const box = { background: 'rgba(30,45,64,0.85)', backdropFilter: 'blur(8px)', border: '1px solid #5080b8', color: '#fff', padding: '1rem', borderRadius: 6, width: 340, maxHeight: '80vh', display: 'flex', flexDirection: 'column' }
const list = { overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: 4 }
const clearBtn = { background: '#3a1a1a', border: '1px solid #c05050', padding: '4px 8px', cursor: 'pointer', color: '#ff8080', fontWeight: 'bold', textAlign: 'left' }
const itemBtn = { display: 'flex', alignItems: 'center', gap: 8, background: '#243a5e', border: '1px solid #3a5a8c', color: '#fff', padding: '4px 8px', cursor: 'pointer', textAlign: 'left' }
