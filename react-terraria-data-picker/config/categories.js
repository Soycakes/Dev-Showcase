// Each axis option runs one dimension of the grid.
// rename: display label overrides (data value -> shown label)
// colors: header text color per value, used for rarity
// order: overwrite alphabetical sort order
export const axisOptions = [
  { label: 'Weapon Class', field: 'damageType', rename: {
    'Ranged': 'Ranger',
    'Magic': 'Mage',
    'Summon': 'Summoner',
  }, order: ['Melee', 'Ranged', 'Magic', 'Summon'] },
  { label: 'Obtained By', field: 'obtained' },
  { label: 'Rarity', field: 'rarity', rename: {
    '0': 'White', '1': 'Blue', '2': 'Green', '3': 'Orange',
    '4': 'Light Red', '5': 'Pink', '6': 'Light Purple',
    '7': 'Lime', '8': 'Yellow', '9': 'Cyan', '10': 'Red',
  }, colors: {
    '0': '#ffffff', '1': '#9696ff', '2': '#96ff96', '3': '#ffc896',
    '4': '#ff9696', '5': '#ff96ff', '6': '#d2a0ff',
    '7': '#96ff0a', '8': '#ffff00', '9': '#05c3ff', '10': '#ff2020',
  }, order: ['0','1','2','3','4','5','6','7','8','9','10'] },
  { label: 'Knockback', field: 'knockback' },
  { label: 'Speed', field: 'speed' },
  { label: 'Subclass', field: 'subclass', order: [
    'Swords',
    'Spears',
    'Yoyos',
    'Boomerangs',
    'Flails',
    'Bows',
    'Guns',
    'Launchers',
    'Specialist',
    'Wands',
    'Magic Guns',
    'Spell Tomes',
    'Summons',
    'Sentries',
    'Whips',
  ]},
  { label: 'Unlocked', field: 'progression tier', rename: {
    'Pre-Wall of Flesh': 'Pre-WoF',
    'Pre-mechanical bosses': 'Pre-Mech',
    'Pre-Lunatic Cultist': 'Pre-Cultist',
  }, order: [
    'Pre-Bosses',
    'Pre-Skeletron',
    'Pre-Wall of Flesh',
    'Pre-mechanical bosses',
    'Pre-Plantera',
    'Pre-Golem',
    'Pre-Lunatic Cultist',
    'Pre-Moon Lord',
    'Endgame',
  ]},
]
