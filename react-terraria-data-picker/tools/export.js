import { writeFileSync } from 'fs'
import { weaponData } from '../src/data/weapons.js'
import { weaponExtras } from '../src/data/weaponsExtra.js'

const weapons = weaponData.map(w => ({
  rawName: w.rawName,
  data: { ...w.data, ...(weaponExtras[w.rawName] || {}) }
}))

const knownFields = ['id', 'name', 'damageType', 'damage', 'knockback', 'speed', 'autoswing', 'rarity', 'sell', 'obtained', 'material', 'tooltip']
const extraKeys = new Set()
for (const w of weapons) {
  for (const key of Object.keys(w.data)) {
    if (!knownFields.includes(key)) extraKeys.add(key)
  }
}
const headers = ['rawName', ...knownFields, ...extraKeys]

function serialize(value) {
  if (value === undefined || value === null) return ''
  if (Array.isArray(value)) return value.join(' | ')
  return String(value)
}

function cell(str) {
  if (str.includes(',') || str.includes('"') || str.includes('\n')) {
    return '"' + str.replace(/"/g, '""') + '"'
  }
  return str
}

const lines = [
  headers.join(','),
  ...weapons.map(w =>
    headers.map(h => cell(serialize(h === 'rawName' ? w.rawName : w.data[h]))).join(',')
  )
]

writeFileSync('tools/weapons.csv', lines.join('\n'), 'utf8')
console.log(`Exported ${weapons.length} weapons to tools/weapons.csv`)
