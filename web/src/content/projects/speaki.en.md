<a href="https://youtu.be/OAPuY1m37U0" target="_blank" rel="noreferrer">Watch the trailer</a>

Speaki the Chess is an auto battler that runs in a browser tab with nothing to download. It's a fan game set in the Trickcal Revive universe, an unofficial project with no affiliation with Epid Games. Each round you spend gold in a shop, put units on a 3x3 grid, equip artifacts and cast spells. When the timer runs out, your board fights an opponent on its own. There are 1v1 and 4 player lobbies and a solo mode against AI, and it's in beta with real players testing it. My original goal was to host 500 players on a single $5 a month VPS, and that goal is the reason the server is written in Go, since it's fast and light.

![The combat board today|full](/Dev-Showcase/media/SPK_26_combat_board_today.webp)

I built it alone, a Unity 6 WebGL client and a Go WebSocket server with SQLite. I wanted it to feel like a real online game, with accounts, matchmaking, a ladder, daily quests and bots.

## From a sketch to a board

I started with a drawing. It was a sketch of how the UI could look like Trickcal's own, so the game would feel like part of that world. After that came white boxes in Unity for the earliest playable slice, then a scene where one Speaki portrait stood in for every unit, then labeled combat panels with no art at all.

![My own UI sketch, to keep the look close to Trickcal's](/Dev-Showcase/media/SPK_01_sketch.webp)
![The earliest playable slice](/Dev-Showcase/media/SPK_02_first_slice.webp)

![Placeholder Speakis standing in for every unit](/Dev-Showcase/media/SPK_03_placeholder_units.webp)
![Early combat panels, no art yet](/Dev-Showcase/media/SPK_04_combat_panels.webp)

Next came the real art. I pulled in a lot of Trickcal reference art of all kinds, and my first pass had no proper scaling, so units came in at all sizes. Going through every image by hand would have been slow, so I wrote a simple set of resizing rules that made nearly all of them look natural in their slots. Only a few of the 100 plus images needed a manual touch up afterwards.

![First pass at the art, before proper scaling](/Dev-Showcase/media/SPK_05_art_scaling.webp)
![Panels with real unit art](/Dev-Showcase/media/SPK_08_panels_with_art.webp)

![One sitting of polishing the game images|lg](/Dev-Showcase/media/SPK_11_polish_sitting.webp)

## The server is the referee

Go simulates every fight and sends the client a flat list of events, and the Unity client just replays that list as animation. Players can't cheat combat results because the client never decides them, and there's only one combat implementation to maintain.

It didn't start this way. My first version had the client recompute each fight from a shared seed, since that sends almost nothing over the network. In practice I had to write the same combat rules in both Go and C#, I was chasing desyncs constantly, and both copies got worse for it. I dropped it and put the effort into packaging the combat and animation data so the server could send it compactly. That kept the bandwidth small and got rid of the duplicate simulator.

## Cards are rows in a spreadsheet

Every ability is a row of data. It has a trigger (when), an action (what), a target (who) and some numbers. A row like `OnBattleStart, Buff, RandomAllies, 2, 2` reads as "at the start of combat, give two random allies +2". There are no per card scripts. The 170+ cards (79 minions, 51 artifacts, 34 spells and 5 tokens) and 44 heroes are all written this way, including Senior upgrades at 4 stars, Yearning upgrades at 6 stars, usage caps and stackable artifacts. A Python script turns the design spreadsheets into the JSON the game loads, so a new card never touches code.

![The minion sheet, one effect per row|full](/Dev-Showcase/media/SPK_28_minion_sheet.webp)

The sheets are the source of truth. Each card is a few rows with one effect per row, colored by skill. Blue is the base ability, orange is Senior and purple is Yearning. Triggers, actions and targets are dropdowns, and even the animation is a column, so every effect names its own clip. Go tests check every card file, so a bad row gets caught before it reaches the game. Below are the trigger and action lists, color coded by set as I add new ones, and the balancing sheet.

![The trigger and action lists, color coded by set](/Dev-Showcase/media/SPK_10_sheet_triggers_actions.webp)
![The start of balancing the card sheet](/Dev-Showcase/media/SPK_06_sheet_balancing.webp)

The shop and combat share one dispatcher and one set of handlers, so a buff does the same thing when you buy a card as it does mid fight, and one set of tests covers both. Card text on screen is generated from the same rows, in English, Korean and Japanese, so a description can't disagree with what the card actually does. One tooltip builder draws every description in the game, and the build script fails if UI code tries to build card text on its own.

## Seeded matches and a test net

All shop and bot randomness comes from one match seed, split into named streams by round and seat. The same seed gives the same hero offers, shops and bot decisions, which turned "I can't reproduce that bug" into "run seed 12345". A test scans the match code and fails the build if anything calls global random.

The bigger safety net is a golden master suite. It runs 2,000 fights built from the real card pool (1 to 6 stars, shields, artifacts, spells, heroes, late game boards) and compares them byte for byte against recorded transcripts. I checked it by planting small bugs on purpose to see if it caught them. On top of that there's a test for every trigger and every action, validation of all the card data, and seeded bot matches pinned end to end.

## Making it fit on a $5 server

I treated performance as its own project. Every change was one hypothesis, one change, one benchmark, and a golden test pass before I kept it. Profiling showed 88% of allocated memory in one place, where the server copied whole buff lists after every effect to work out what changed for the replay. Capturing only the fields that matter and copying only the units that changed made live fights 3.5x faster and 5.2x lighter.

That change had a cost I didn't expect. The bots score their moves by running small fights, and for those the new captured data was too big and got copied on every call, which made them about 7% slower. Bots use most of the server's CPU, so after a 20 run comparison I reverted the change and redid it, putting the extra comparison data behind a pointer that light fights never allocate. I kept the failed attempt and my reasoning in the performance log.

Across 20+ single change optimizations, a live fight went from 1.32 ms to 0.268 ms (4.9x faster, 6.5x less memory), and the hardest bot turn went from 28.5 ms to 18.3 ms (36% faster, 48% less memory). The server's target is 500 concurrent players on that one VPS, which is where this all started.

## Bots that follow the real rules

Solo mode has several difficulty levels. The easiest bot plays random legal moves. The hardest one tries moves through the real shop handlers on a cloned seat, scores each result by running quick fights, screens candidates cheaply and then confirms the best ones. Since bots run the same code players do, they can't make an illegal move and they understand every new card as soon as it exists. Bot decks come from plain text deck exports, so adding one means dropping in a file.

## UI and UX

Everything above is the engine, and players never see it. What they see is the UI, so this part is about how the game looks and reads on screen.

### Card panels and the guide

The panels that show a unit's Senior and Yearning upgrades went through several passes. The layout was close to today's early on, and most of the later work was choosing the wording for an upgrade to a base ability.

![Placeholder panels, already close to today's layout|lg](/Dev-Showcase/media/SPK_20_card_panels_placeholder.webp)

![Choosing the wording for Senior and Yearning](/Dev-Showcase/media/SPK_21_senior_yearning_wording.webp)
![The cleaned up version](/Dev-Showcase/media/SPK_24_yearning_cleanup.webp)

The in game guide teaches the rules with examples and is loaded from JSON. Its images are pooled and pulled from Addressables assets too, so any image I choose can go into the guide.

![The in game guide|lg](/Dev-Showcase/media/SPK_12_guide.webp)

### The deck builder

The deck builder is organized by tier, with a counter for how many apostles, artifacts and spells you've picked. The first panel was a plain scroll of portraits. The cleanup turned it into tabs and colored bars, with copy and paste buttons.

![The first deck picker panel](/Dev-Showcase/media/SPK_07_deck_picker_start.webp)
![The artifact picker](/Dev-Showcase/media/SPK_14_deck_picker_artifacts.webp)
![After the cleanup](/Dev-Showcase/media/SPK_17_deck_picker_cleanup.webp)

### Artifacts

Each minion can carry three artifacts, and the UI makes that clear. Buying three of the same artifact turned out to be too strong in some cases, so I added optional rules for artifacts. You can still equip several copies of the same one, but only one ability triggers.

![A minion carrying three copies of an artifact|sm](/Dev-Showcase/media/SPK_18_artifact_duplicates.webp)

### Readable buff rows

A unit can carry a lot of buffs at once, and the list gets ugly quickly. The fix was an order. Artifacts always sit at the top. Below them the buffs are ranked by a custom rule, with debuffs first, then special buffs, then plain value buffs. Among the value buffs, the one with the larger attack or health value comes first, ties go to the lower of the other value, and a few more rules decide the rest.

![A long buff list that was hard to read|sm](/Dev-Showcase/media/SPK_13_buff_rows.webp)

### Three languages

The game comes in English, Korean and Japanese. Every string is a key with an English, Korean and Japanese column, and placeholders like `{n}` and `{card.tier}` are filled in by the game. Some Japanese cells are still empty, and a comment marks one that needs review.

![The translation sheet|lg](/Dev-Showcase/media/SPK_27_translation_sheet.webp)

The hard part was fonts, making sure every language's characters exist in the font and still render correctly. This glyph test sheet is from that work. Some screenshots on this page mix English and Korean text, and they're straight from the game as I built it.

![Checking that the font covers every character|xs](/Dev-Showcase/media/SPK_16_font_glyphs.webp)

### Animation and the client

The client predicts the simple shop actions (buy, sell, move, lock) locally so the UI feels instant, and the next server snapshot overwrites the prediction. Combat visuals come from about 40 reusable clip classes and 53 JSON entries that say which clip, sprite, colors and duration to use. The clip timings feed back into the server's match clock, so the deadline accounts for how long a fight takes to play. Clips are written against a tween builder, which is what makes 2x and 3x speed and skip work everywhere without special cases. The verification script rejects clip code that would break that, such as raw coroutines or direct time use.

When I started on more UI art I looked at hit feedback first, and added a splash when a minion takes damage. This is also the first view of the polished combat board.

![The hit splash and the first polished combat board|lg](/Dev-Showcase/media/SPK_19_hit_splash.webp)

Each minion's shadow changes color during combat. At the start of battle it's gray while idle, and green or blue depending on whether that minion's start of combat effect is going off or waiting for its turn in the queue. Once the fight starts, the same colors show who's attacking.

![The shadow colored for combat](/Dev-Showcase/media/SPK_22_shadow_color.webp)

![The shadow's natural look|tiny](/Dev-Showcase/media/SPK_23_shadow_rest.webp)

Banners and more polish came after that, like this banner at the start of combat.

![The start of combat banner|lg](/Dev-Showcase/media/SPK_25_banner.webp)

## Accounts, ladder and security

Players have accounts with bcrypt passwords, one time recovery codes and session checks on every route. Login for a missing user takes the same time as a wrong password, so an attacker can't tell the two apart. There's rate limiting per IP and per connection, exponential backoff after failed attempts, capped registration, and WebSocket limits on message size and origin. Usernames go through a profanity filter before they're stored, and a SECURITY.md tracks what's covered and what's still open.

On top of that sit the live systems. There's a ranked ladder with 1v1 and 4 player tabs, daily quests that reset lazily at 4 AM KST, currencies and rewards, solo runs that save and resume (a second tab takes over cleanly), and a heartbeat that drives the live player count.

## Tools that keep the sheets and the game in sync

The design lives in Google Sheets, but the game reads JSON, so I wrote small tools to carry the data both ways. `csv_to_json.py` turns the minion, spell, artifact, token and hero sheets into the card JSON files. It also handles small traps, like Unity's JsonUtility reading a missing bool as false, so artifacts always spell out whether they can stack.

Text has its own script, `tools/locale.py`, with three modes. `generate` reads every card and hero into the English locale file and keeps the existing key order, so pasting back into the sheet never shifts a row. `export` writes English, Korean and Japanese into one CSV, with a type column for filtering and a context column, ready to paste into the sheet. `import` reads the sheet back into the three locale files.

Card text is generated from the rows, so the only way to review it is to read it. A Unity menu tool called Dump Card Text writes every card's name and its generated base, Senior and Yearning text, in English and Korean, into one CSV in the repo. When I change a text template, the git diff shows exactly which cards changed, and the same file pastes into a sheet for proofreading.

![The card text dump, English and Korean side by side|full](/Dev-Showcase/media/SPK_29_card_text_dump.webp)

Another editor tool lists every TextMeshPro object in a scene that isn't wired to the localization system, so a hardcoded string has nowhere to hide. A localization audit lists the English strings that were hardcoded in the UI code, each with its file, line and a suggested key, and those strings now live in the sheet as keys. The `--all` flag on `check.ps1` regenerates all the JSON from the sheets and imports the locale before it runs the usual checks.

None of these is big, but together they mean a card change goes from the sheet to the screen without anyone editing JSON by hand, and every line of text that reaches the screen can be proofread.

## Tooling and how I worked

A combat test lab lets me possess any board, grant any card, submit it and replay the result in the real client. One PowerShell script, `check.ps1`, syncs the data into Unity, checks that the Go and C# content versions match, lints the animation data and clip code, enforces the UI rules, then builds and tests the server. That script is what makes a codebase this big safe to change quickly.

I used Claude Code throughout, under a written contract in CLAUDE.md that sets who decides what, the style rules and the verification steps, along with living docs for technical notes, performance, security and playtest setup. I wrote the design docs first. With those in place, the framework and boilerplate came together in about two days, and within three days of finishing the design I had the full loop running, with matchmaking, shop phase and combat. Anything an agent wrote only went in once it passed the project rules, the tests and the check script.

## Deployment

The WebGL build is on Cloudflare Pages. Card art and audio come as Addressables bundles from Cloudflare R2, behind a Worker that checks a token so they can't be scraped. The Go server runs on a small VPS behind a Cloudflare Tunnel, which gives it a permanent wss address without opening inbound ports, and stores data in SQLite in WAL mode. Continuous replication with Litestream is planned. I wrote the playtest and release guide so a friend could get a build running from scratch, and I've run playtests with real players.

![The Addressables groups in Unity|full](/Dev-Showcase/media/SPK_09_addressables.webp)

## Future plans

The beta is still going strong. Every playtest turns up new ideas, and the first one already did, since it's why the game has a ready up indicator now. I plan to add many more features, aiming for nearly double today's content as more playtests reveal better ones.

![The ready up indicator, added after the first playtest|tiny](/Dev-Showcase/media/SPK_15_ready_up.webp)
