import { readFileSync, writeFileSync } from 'fs'
import { weaponData } from '../src/data/weapons.js'

const ORIGINAL_FIELDS = new Set(['rawName', 'id', 'name', 'damageType', 'damage', 'knockback', 'speed', 'autoswing', 'rarity', 'sell', 'obtained', 'material', 'tooltip'])
const existingNames = new Set(weaponData.map(w => w.rawName))

function parseRow(line) {
  const cells = []
  let i = 0
  while (i < line.length) {
    if (line[i] === '"') {
      let value = ''
      i++
      while (i < line.length) {
        if (line[i] === '"' && line[i + 1] === '"') { value += '"'; i += 2 }
        else if (line[i] === '"') { i++; break }
        else value += line[i++]
      }
      cells.push(value)
      if (line[i] === ',') i++
    } else {
      const end = line.indexOf(',', i)
      if (end === -1) { cells.push(line.slice(i)); break }
      cells.push(line.slice(i, end))
      i = end + 1
    }
  }
  return cells
}

function parseValue(str) {
  if (str === '') return undefined
  const lower = str.toLowerCase()
  if (lower === 'true') return true
  if (lower === 'false') return false
  if (str.includes('|')) return str.split('|').map(s => s.trim()).filter(Boolean)
  return str
}

const text = readFileSync('tools/weapons.csv', 'utf8').replace(/\r/g, '')
const [headerLine, ...dataLines] = text.trim().split('\n')
const headers = parseRow(headerLine)

const extras = {}
const newWeapons = []

for (const line of dataLines) {
  const row = parseRow(line)
  const rawName = row[0]
  if (!rawName) continue

  const extra = {}
  headers.forEach((h, i) => {
    if (ORIGINAL_FIELDS.has(h)) return
    const parsed = parseValue(row[i] ?? '')
    if (parsed !== undefined) extra[h] = parsed
  })
  if (Object.keys(extra).length > 0) extras[rawName] = extra

  if (!existingNames.has(rawName)) {
    const data = {}
    headers.forEach((h, i) => {
      if (h === 'rawName' || !ORIGINAL_FIELDS.has(h)) return
      const parsed = parseValue(row[i] ?? '')
      if (parsed !== undefined) data[h] = parsed
    })
    newWeapons.push({ rawName, data })
  }
}

const extrasBody = Object.entries(extras).map(([rawName, data]) => {
  const fields = Object.entries(data).map(([k, v]) => {
    const key = /^[a-zA-Z_$][a-zA-Z0-9_$]*$/.test(k) ? k : `"${k}"`
    return `    ${key}: ${JSON.stringify(v)}`
  }).join(',\n')
  return `  "${rawName}": {\n${fields}\n  }`
}).join(',\n')

writeFileSync('src/data/weaponsExtra.js', `export const weaponExtras = {\n${extrasBody}\n}\n`, 'utf8')
console.log(`Imported extras for ${Object.keys(extras).length} weapons into src/data/weaponsExtra.js`)

const newBody = newWeapons.map(w => {
  const fields = Object.entries(w.data).map(([k, v]) => `      ${k}: ${JSON.stringify(v)}`).join(',\n')
  return `  {\n    rawName: ${JSON.stringify(w.rawName)},\n    data: {\n${fields}\n    }\n  }`
}).join(',\n')

writeFileSync('src/data/weaponsNew.js', `export const weaponsNew = [\n${newBody}\n]\n`, 'utf8')
console.log(`Found ${newWeapons.length} new weapons, written to src/data/weaponsNew.js`)
