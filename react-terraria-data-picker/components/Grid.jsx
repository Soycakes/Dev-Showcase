import Sprite from './Sprite'

function display(axis, value) {
  return axis?.rename?.[value] || value
}

export default function Grid({ rows, cols, picks, emptyCells, rowAxis, colAxis, favorites, colFavorites, totalFavorite, onCellClick, onFavoriteClick, onColFavoriteClick, onTotalFavoriteClick }) {
  if (!rows || !cols) {
    return <p style={{ padding: '1rem' }}>Pick a row and column category to get started.</p>
  }

  return (
    <div style={{ overflowX: 'auto', padding: '1rem', backdropFilter: 'blur(6px)' }}>
      <table style={{ borderCollapse: 'collapse', tableLayout: 'fixed', width: 'max-content' }}>
        <thead>
          <tr>
            <th style={corner} />
            {cols.map(col => (
              <th key={col} style={{ ...header, color: colAxis?.colors?.[col] || '#fff' }}>{display(colAxis, col)}</th>
            ))}
            <th style={header}>Favorite</th>
          </tr>
        </thead>
        <tbody>
          {rows.map(row => {
            const favorite = favorites[row]
            return (
              <tr key={row}>
                <td style={{ ...header, color: rowAxis?.colors?.[row] || '#fff' }}>{display(rowAxis, row)}</td>
                {cols.map(col => {
                  const pick = picks[`${row}|${col}`]
                  return (
                    <td key={col} className="grid-cell" style={emptyCells.has(`${row}|${col}`) ? emptyCell : cell} onClick={() => onCellClick(row, col)}>
                      <div style={cellInner}>
                        {pick && <Sprite id={pick.data.id} rawName={pick.rawName} name={pick.data.name} size={64} />}
                      </div>
                    </td>
                  )
                })}
                <td className="fav-cell" style={favoriteCell} onClick={() => onFavoriteClick(row)}>
                  <div style={cellInner}>
                    {favorite && <Sprite id={favorite.data.id} rawName={favorite.rawName} name={favorite.data.name} size={64} />}
                  </div>
                </td>
              </tr>
            )
          })}
          <tr>
            <td style={header}>Favorite</td>
            {cols.map(col => {
              const fav = colFavorites[col]
              return (
                <td key={col} className="fav-cell" style={favoriteCell} onClick={() => onColFavoriteClick(col)}>
                  <div style={cellInner}>
                    {fav && <Sprite id={fav.data.id} rawName={fav.rawName} name={fav.data.name} size={64} />}
                  </div>
                </td>
              )
            })}
            <td className="tot-cell" style={totalCell} onClick={onTotalFavoriteClick}>
              <div style={cellInner}>
                {totalFavorite && <Sprite id={totalFavorite.data.id} rawName={totalFavorite.rawName} name={totalFavorite.data.name} size={64} />}
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  )
}

const corner = { width: 90, height: 60 }
const header = { width: 90, padding: '8px 6px', background: 'rgba(58,90,140,0.95)', color: '#fff', fontWeight: 'bold', border: '1px solid #5080b8', textAlign: 'center' }
const cell = { width: 90, height: 80, border: '1px solid #3a5a8c', background: 'rgba(36,58,94,0.9)', cursor: 'pointer', verticalAlign: 'middle' }
const emptyCell = { width: 90, height: 80, border: '1px solid #1e2d40', background: 'rgba(17,24,39,0.9)', cursor: 'pointer', verticalAlign: 'middle' }
const favoriteCell = { width: 90, height: 80, border: '2px solid #5080b8', background: 'rgba(45,79,122,0.85)', cursor: 'pointer', verticalAlign: 'middle' }
const totalCell = { width: 90, height: 80, border: '2px solid #4070a0', background: 'rgba(30,58,90,0.85)', cursor: 'pointer', verticalAlign: 'middle' }
const cellInner = { display: 'flex', alignItems: 'center', justifyContent: 'center', width: '100%', height: '100%' }
