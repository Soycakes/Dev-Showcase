export const STAR_CHARS = ['·', '+', '*', '✦', '◦', '⊹', '∗', '✶', '✷', '✸', '❋']
export const BIG_STAR_CHARS = ['✶', '✷', '✸', '✹', '❋', '✼']

export const MORPH_SEQS: string[][] = [
  ['·', '∘', '○', '◎', '○', '∘'],
  ['✦', '✧', '✩', '★', '✩', '✧'],
  ['⊹', '✶', '✷', '✸', '✷', '✶'],
  ['∗', '✲', '✳', '✴', '✳', '✲'],
]

export const PLANETS: string[][] = [
  ['  . o .', ' ( *** )', "  ' o '"],
  ['  _____', ' /     \\', '|  o    |', '|   ~~~ |', ' \\_____/'],
  ['    ___    ', '~~(  o  )~~', '    ---    '],
]

export const ROCKET_LINES = [
  '     /\\',
  '    /  \\',
  '   |====|',
  '   | () |',
  '   |    |',
  '  /|    |\\',
  ' /_|    |_\\',
  '   |====|',
]

export const FLAME_FRAMES: string[][] = [
  ['    ) (  ', '    ( )   '],
  ['    ( ) ) ', '    ) (  '],
  ['     ) ((  ', '    (  )  '],
]

export const CFG = {
  totalStars: 180,
  morphingCount: 20,
  bigStarCount: 25,

  fontSize: 13,
  planetLineHeight: 1.5,

  rotationPerMs: 0.000018, // radians/ms, ~5.8 min per full rotation
  dtCap: 50,
  morphStepMs: 700,

  twinkleDim: 0.55,
  twinkleSwing: 0.45,
  starSpeed: [0.3, 1.2] as [number, number],

  starOpacity: [0.12, 0.38] as [number, number],
  bigStarOpacity: [0.25, 0.50] as [number, number],
  planetOpacity: [0.10, 0.20] as [number, number],

  bigStarScale: [1.4, 2.0] as [number, number],

  planetDistRatio: [0.3, 0.6] as [number, number],

  lightModeStarAlpha: 0.4,
  lightModePlanetAlpha: 0.3,

  rocketScale: 0.5,
  rocketLerp: 0.005,
  rocketMaxTiltDeg: 80,
  rocketTiltDistance: 150, // px of horizontal offset for max tilt
  rocketOpacity: 0.4,
  flameOpacity: 0.2,
  flameStepMs: 500,

  // must match body background in index.css
  bgDark: '#0a0a0a',
  bgLight: '#ffffff',

  colors: {
    starDark: '#c8d8f0',
    starLight: '#1a2040',
    planetDark: '#8899bb',
    planetLight: '#2a3a6a',
    flameDark: '#ffaa44',
    flameLight: '#cc6600',
  },
}
