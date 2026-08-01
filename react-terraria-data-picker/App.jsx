import { useState, useEffect, useMemo } from 'react'
import { weaponData } from './data/weapons'
import { weaponsNew } from './data/weaponsNew'
import { weaponExtras } from './data/weaponsExtra'
import { axisOptions } from './config/categories'
import Grid from './components/Grid'
import CellModal from './components/CellModal'
import FavoritePicker from './components/FavoritePicker'

const weapons = [...weaponData, ...weaponsNew].map(w => ({
  ...w,
  data: { ...w.data, ...(weaponExtras[w.rawName] || {}) }
}))

const SAVE_KEY = 'tgrid'

function getUniqueValues(axis) {
  const all = weapons.flatMap(w => {
    const val = w.data[axis.field]
    return Array.isArray(val) ? val : [val]
  })
  const unique = [...new Set(all)].filter(Boolean)
  if (axis.order) return unique.sort((a, b) => {
    const ai = axis.order.indexOf(a)
    const bi = axis.order.indexOf(b)
    return (ai === -1 ? Infinity : ai) - (bi === -1 ? Infinity : bi)
  })
  return unique.sort((a, b) => a.localeCompare(b, undefined, { numeric: true }))
}

function slotKey(rowField, colField) {
  return `${rowField || ''}|${colField || ''}`
}

function deserializeSlot(slot) {
  const byId = id => weapons.find(w => w.data.id === id) || null
  const picks = {}
  for (const [key, id] of Object.entries(slot.picks || {})) {
    const w = byId(id)
    if (w) picks[key] = w
  }
  const favorites = {}
  for (const [row, id] of Object.entries(slot.favorites || {})) {
    const w = byId(id)
    if (w) favorites[row] = w
  }
  const colFavorites = {}
  for (const [col, id] of Object.entries(slot.colFavorites || {})) {
    const w = byId(id)
    if (w) colFavorites[col] = w
  }
  return {
    picks,
    favorites,
    colFavorites,
    totalFavorite: byId(slot.totalFavorite),
  }
}

function serializeSlot(picks, favorites, colFavorites, totalFavorite) {
  const savedPicks = {}
  for (const [k, w] of Object.entries(picks)) savedPicks[k] = w.data.id
  const savedFavorites = {}
  for (const [row, w] of Object.entries(favorites)) savedFavorites[row] = w.data.id
  const savedColFavorites = {}
  for (const [col, w] of Object.entries(colFavorites)) savedColFavorites[col] = w.data.id
  return {
    picks: savedPicks,
    favorites: savedFavorites,
    colFavorites: savedColFavorites,
    totalFavorite: totalFavorite?.data.id || null,
  }
}

const init = (() => {
  try {
    const all = JSON.parse(localStorage.getItem(SAVE_KEY) || '{}')
    const rowAxis = axisOptions.find(o => o.field === all.rowAxis) || axisOptions.find(o => o.field === 'subclass')
    const colAxis = axisOptions.find(o => o.field === all.colAxis) || axisOptions.find(o => o.field === 'progression tier')
    const slot = all.slots?.[slotKey(rowAxis?.field, colAxis?.field)] || {}
    return { rowAxis, colAxis, ...deserializeSlot(slot) }
  } catch {
    return {
      rowAxis: axisOptions.find(o => o.field === 'subclass'),
      colAxis: axisOptions.find(o => o.field === 'progression tier'),
      picks: {},
      favorites: {},
      colFavorites: {},
      totalFavorite: null,
    }
  }
})()

export default function App() {
  const [rowAxis, setRowAxis] = useState(init.rowAxis)
  const [colAxis, setColAxis] = useState(init.colAxis)
  const [picks, setPicks] = useState(init.picks)
  const [favorites, setFavorites] = useState(init.favorites)
  const [colFavorites, setColFavorites] = useState(init.colFavorites)
  const [totalFavorite, setTotalFavorite] = useState(init.totalFavorite)
  const [openCell, setOpenCell] = useState(null)
  const [openFavoriteRow, setOpenFavoriteRow] = useState(null)
  const [openFavoriteCol, setOpenFavoriteCol] = useState(null)
  const [openTotalFavorite, setOpenTotalFavorite] = useState(false)

  const rows = useMemo(() => rowAxis ? getUniqueValues(rowAxis) : null, [rowAxis])
  const cols = useMemo(() => colAxis ? getUniqueValues(colAxis) : null, [colAxis])

  const emptyCells = useMemo(() => {
    if (!rows || !cols) return new Set()
    const set = new Set()
    for (const row of rows) {
      for (const col of cols) {
        const has = weapons.some(w => {
          const rv = w.data[rowAxis.field]
          const cv = w.data[colAxis.field]
          return (Array.isArray(rv) ? rv.includes(row) : rv === row) &&
                 (Array.isArray(cv) ? cv.includes(col) : cv === col)
        })
        if (!has) set.add(`${row}|${col}`)
      }
    }
    return set
  }, [rowAxis, colAxis])

  useEffect(() => {
    const all = JSON.parse(localStorage.getItem(SAVE_KEY) || '{}')
    all.rowAxis = rowAxis?.field || null
    all.colAxis = colAxis?.field || null
    all.slots = all.slots || {}
    all.slots[slotKey(rowAxis?.field, colAxis?.field)] = serializeSlot(picks, favorites, colFavorites, totalFavorite)
    localStorage.setItem(SAVE_KEY, JSON.stringify(all))
  }, [rowAxis, colAxis, picks, favorites, colFavorites, totalFavorite])

  function pickAxis(field, isRow) {
    const newRowField = isRow ? (field || null) : (rowAxis?.field || null)
    const newColField = isRow ? (colAxis?.field || null) : (field || null)
    const all = JSON.parse(localStorage.getItem(SAVE_KEY) || '{}')
    const newSlot = deserializeSlot(all.slots?.[slotKey(newRowField, newColField)] || {})
    if (isRow) setRowAxis(axisOptions.find(o => o.field === field) || null)
    else setColAxis(axisOptions.find(o => o.field === field) || null)
    setPicks(newSlot.picks)
    setFavorites(newSlot.favorites)
    setColFavorites(newSlot.colFavorites)
    setTotalFavorite(newSlot.totalFavorite)
  }

  function onPick(weapon) {
    const key = `${openCell.row}|${openCell.col}`
    const current = picks[key]
    setPicks(prev => {
      const next = { ...prev }
      if (weapon) next[key] = weapon
      else delete next[key]
      return next
    })
    if (current && weapon?.rawName !== current.rawName) {
      if (current.rawName === favorites[openCell.row]?.rawName)
        setFavorites(prev => { const next = { ...prev }; delete next[openCell.row]; return next })
      if (current.rawName === colFavorites[openCell.col]?.rawName)
        setColFavorites(prev => { const next = { ...prev }; delete next[openCell.col]; return next })
      if (current.rawName === totalFavorite?.rawName)
        setTotalFavorite(null)
    }
    setOpenCell(null)
  }

  function onFavoritePick(weapon) {
    setFavorites(prev => {
      const next = { ...prev }
      if (weapon) next[openFavoriteRow] = weapon
      else delete next[openFavoriteRow]
      return next
    })
    setOpenFavoriteRow(null)
  }

  function onColFavoritePick(weapon) {
    setColFavorites(prev => {
      const next = { ...prev }
      if (weapon) next[openFavoriteCol] = weapon
      else delete next[openFavoriteCol]
      return next
    })
    setOpenFavoriteCol(null)
  }

  function onTotalFavoritePick(weapon) {
    setTotalFavorite(weapon || null)
    setOpenTotalFavorite(false)
  }

  function reset() {
    if (!confirm('Reset all picks?')) return
    setPicks({})
    setFavorites({})
    setColFavorites({})
    setTotalFavorite(null)
    // keep axes but wipe all saved slots so switching axes doesnt restore old picks
    localStorage.setItem(SAVE_KEY, JSON.stringify({
      rowAxis: rowAxis?.field || null,
      colAxis: colAxis?.field || null,
      slots: {},
    }))
  }

  const totalPool = [...new Map(
    [...Object.values(favorites), ...Object.values(colFavorites)].map(w => [w.rawName, w])
  ).values()]

  return (
    <div style={{ backgroundImage: 'url(/TerrariaWeapons/ui/background.png)', backgroundSize: 'cover', backgroundAttachment: 'fixed', minHeight: '100vh', padding: '1rem', color: '#fff' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '1rem' }}>
        <h1 style={{ color: '#f5c842', fontSize: '3rem' }}>Favorite Terraria Weapons</h1>
        <a href="https://www.youtube.com/@soycake" target="_blank" rel="noreferrer" style={{ color: '#fff', fontSize: '0.9rem', textDecoration: 'none' }}>by Soycake</a>
        <a href="https://www.youtube.com/@soycake" target="_blank" rel="noreferrer" title="YouTube">
          <img src="/TerrariaWeapons/ui/youtubeLogo.png" width="32" height="32" alt="YouTube" style={{ display: 'block' }} />
        </a>
        <a href="https://github.com/Soycakes/TerrariaWeapons" target="_blank" rel="noreferrer" title="GitHub">
          <img src="/TerrariaWeapons/ui/githubLogo.png" width="32" height="32" alt="GitHub" style={{ display: 'block' }} />
        </a>
      </div>

      <div style={{ display: 'inline-flex', gap: '1.5rem', alignItems: 'center', marginBottom: '1rem', background: 'rgba(30,45,64,0.75)', backdropFilter: 'blur(6px)', border: '2px solid #5080b8', borderRadius: 6, padding: '0.6rem 1rem' }}>
        <label style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
          Rows:
          <select value={rowAxis?.field || ''} onChange={e => pickAxis(e.target.value, true)} style={{ background: '#243a5e', color: '#fff', border: '1px solid #5080b8', padding: '2px 6px', borderRadius: 4 }}>
            <option value="">N/A</option>
            {axisOptions.map(o => <option key={o.field} value={o.field} disabled={o.field === colAxis?.field}>{o.label}</option>)}
          </select>
        </label>

        <label style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
          Columns:
          <select value={colAxis?.field || ''} onChange={e => pickAxis(e.target.value, false)} style={{ background: '#243a5e', color: '#fff', border: '1px solid #5080b8', padding: '2px 6px', borderRadius: 4 }}>
            <option value="">N/A</option>
            {axisOptions.map(o => <option key={o.field} value={o.field} disabled={o.field === rowAxis?.field}>{o.label}</option>)}
          </select>
        </label>

        <button onClick={reset} style={{ border: '1px solid #5080b8', background: '#243a5e', color: '#fff', padding: '2px 12px', cursor: 'pointer', borderRadius: 4 }}>Reset</button>
      </div>

      <Grid
        rows={rows}
        cols={cols}
        picks={picks}
        emptyCells={emptyCells}
        rowAxis={rowAxis}
        colAxis={colAxis}
        favorites={favorites}
        colFavorites={colFavorites}
        totalFavorite={totalFavorite}
        onCellClick={(row, col) => setOpenCell({ row, col })}
        onFavoriteClick={row => setOpenFavoriteRow(row)}
        onColFavoriteClick={col => setOpenFavoriteCol(col)}
        onTotalFavoriteClick={() => setOpenTotalFavorite(true)}
      />

      {openCell && (
        <CellModal
          weapons={weapons}
          rowAxis={rowAxis}
          colAxis={colAxis}
          row={openCell.row}
          col={openCell.col}
          onPick={onPick}
          onClose={() => setOpenCell(null)}
        />
      )}

      {openFavoriteRow && (
        <FavoritePicker
          title="Pick row favorite"
          options={cols ? cols.map(col => picks[`${openFavoriteRow}|${col}`]).filter(Boolean) : []}
          onPick={onFavoritePick}
          onClose={() => setOpenFavoriteRow(null)}
        />
      )}

      {openFavoriteCol && (
        <FavoritePicker
          title="Pick column favorite"
          options={rows ? rows.map(row => picks[`${row}|${openFavoriteCol}`]).filter(Boolean) : []}
          onPick={onColFavoritePick}
          onClose={() => setOpenFavoriteCol(null)}
        />
      )}

      {openTotalFavorite && (
        <FavoritePicker
          title="Pick total favorite"
          options={totalPool}
          onPick={onTotalFavoritePick}
          onClose={() => setOpenTotalFavorite(false)}
        />
      )}
    </div>
  )
}
