Speaki the Chess is an auto battler that runs in a browser tab with nothing to download. It is a fan game set in the Trickcal Revive universe. Each round you spend gold in a shop, place units on a 3x3 grid, equip artifacts and cast spells. When the timer ends, your board fights an opponent automatically. It supports 1v1, 4 player lobbies and a solo mode against AI, and it is in beta with real players testing it.

I built it alone: a Unity 6 WebGL client and a Go WebSocket server with SQLite, about 35K lines across both. The goal was to build something that feels like a shipped online game, with accounts, matchmaking, a ladder, daily quests and bots, and to keep it cheap enough to run on a $5 a month VPS.

## The server is the referee

Go simulates every fight and sends the client a flat list of events. The Unity client only replays that list as animation. Players can't cheat combat results because the client never decides them, and there is one combat implementation to maintain.

It did not start this way. My first version had the client recompute each fight from a shared seed, since that sends almost nothing over the network. In practice it meant writing the same combat rules in both Go and C# and chasing desyncs constantly, and both copies got worse for it. I dropped it and spent the effort on packaging the combat and animation data so the server could send it compactly. That kept the bandwidth small, and it removed the duplicate simulator.

## Cards are rows in a spreadsheet

Every ability is a row of data: a trigger (when), an action (what), a target (who) and some numbers. A row like `OnBattleStart, Buff, RandomAllies, 2, 2` reads as "at the start of combat, give two random allies +2". There are no per-card scripts. The 170+ cards (79 minions, 51 artifacts, 34 spells and 5 tokens) and 44 heroes are all authored this way, including star upgrades at 4 and 6 stars, usage caps and stackable artifacts. A Python script converts the design spreadsheets into the JSON the game loads, so a new card never touches code.

The shop and combat share one dispatcher and one set of handlers, so a buff behaves the same when you buy a card as it does mid fight, and one set of tests covers both. Card text on screen is generated from the same rows, in English, Korean and Japanese, so a card's description can't disagree with what it actually does. One tooltip builder draws every description in the game, and the build script fails if UI code tries to build card text on its own.

## Seeded matches and a test net

All shop and bot randomness comes from one match seed, split into named streams by round and seat. The same seed gives the same hero offers, shops and bot decisions, which turned "I can't reproduce that bug" into "run seed 12345". A test scans the match code and fails the build if anything calls global random.

The bigger safety net is a golden master suite: 2,000 fights built from the real card pool (1 to 6 stars, shields, artifacts, spells, heroes, late game boards), compared byte for byte against recorded transcripts. I checked it by planting small bugs on purpose and confirming it caught them. On top of that there is a test for every trigger and every action, validation of all the card data, and seeded bot matches pinned end to end.

## Making it fit on a $5 server

I treated performance as its own project: one hypothesis, one change, one benchmark, and a golden test pass before anything was kept. Profiling showed 88% of allocated memory in a single place, where the server copied whole buff lists after every effect to work out what changed for the replay. Capturing only the fields that matter and copying only the units that changed made live fights 3.5x faster and 5.2x lighter.

That change also had a cost I didn't expect. The bots score their moves by running small fights, and for those the new captured data was too large and got copied on every call, which made them about 7% slower. Bots use most of the server's CPU, so I reverted the change after a 20 run A/B comparison and redid it, moving the extra comparison data behind a pointer that light fights never allocate. I kept the failed attempt and the reasoning in the performance log.

Across 20+ single change optimizations, a live fight went from 1.32 ms to 0.268 ms (4.9x faster, 6.5x less memory), and the hardest bot turn from 28.5 ms to 18.3 ms (36% faster, 48% less memory). The server targets a budget of 500 concurrent players on that one VPS.

## Bots that follow the real rules

Solo mode has several difficulty tiers. The easiest bot plays random legal moves. The hardest one tries moves through the real shop handlers on a cloned seat, scores each result by running quick fights, screens candidates cheaply and then confirms the best ones. Since bots run the same code players do, they can't make an illegal move and they understand every new card as soon as it exists. Bot decks are loaded from plain text deck exports, so adding one is dropping in a file.

## Animation and the client

The client predicts the simple shop actions (buy, sell, move, lock) locally so the UI feels instant, and the next server snapshot overwrites the prediction. Combat visuals come from about 40 reusable clip classes and 53 JSON entries that say which clip, sprite, colors and duration to use. The clip timings feed back into the server's match clock, so the deadline accounts for how long a fight takes to play. Clips are written against a tween builder, which is what makes 2x and 3x speed and skip work everywhere without special cases. The verification script rejects clip code that would break that, such as raw coroutines or direct time use.

## Accounts, ladder and security

Players have accounts with bcrypt passwords, one time recovery codes and session checks on every route. Login for a missing user costs the same time as a wrong password, so an attacker can't tell the two apart. There is per IP and per connection rate limiting, exponential backoff after failed attempts, capped registration, and WebSocket limits on message size and origin. Usernames go through a profanity filter before they are stored, and a SECURITY.md tracks what is mitigated and what is still open.

On top of that sit the live systems: a ranked ladder with 1v1 and 4 player tabs, daily quests that reset lazily at 04:00 KST, currencies and rewards, solo runs that save and resume (a second tab takes over cleanly), and a heartbeat that drives the live player count.

## Tooling and how I worked

A combat test lab lets me possess any board, grant any card, submit it and replay the result in the real client. One PowerShell script, `check.ps1`, syncs the data into Unity, checks that the Go and C# content versions match, lints the animation data and clip code, enforces the UI rules, and then builds and tests the server. That script is what makes a codebase this large safe to change quickly.

I used Claude Code throughout, under a written contract in CLAUDE.md that sets who decides what, the style rules and the verification steps, along with living docs for technical notes, performance, security and playtest setup. I wrote the design docs first. With those in place, the framework and boilerplate came together in about two days, and within three days of finishing the design I had the full loop running: matchmaking, shop phase and combat. Anything an agent wrote only went in once it passed the project rules, the tests and the check script.

## Deployment

The WebGL build is on Cloudflare Pages. Card art and audio come as Addressables bundles from Cloudflare R2, behind a Worker that checks a token so they can't be scraped. The Go server runs on a small VPS behind a Cloudflare Tunnel, which gives it a permanent wss:// address without opening inbound ports, and stores data in SQLite in WAL mode. Continuous replication with Litestream is planned. I wrote the playtest and release guide so a friend could get a build running from scratch, and I've run playtests with real players.
