I'm building a fan MMORPG based on Trickcal Revive. It is an unofficial fan project and has no affiliation with Epid Games. The inspiration is MapleStory 2, a voxel MMORPG with very simple chibi models and flat texture mapping. Eve, the character below, is a good example of that look.

![Eve from MapleStory 2, the look I'm aiming for|sm](/Dev-Showcase/media/VMP_h00_maple2.webp)

That simplicity is what the whole project leans on. Most of this page is about one part of it, the pipeline that turns a single piece of 2D character art into a rigged, textured, animated low poly 3D character for Unity. I started this project in October 2026, so treat this as a prototype and a record of what I've learned so far, not a finished game. The test character throughout is Kidian, and this is where she ended up:

![Kidian, rendered from the game's own assets](/Dev-Showcase/media/VMP_turntable.mp4)

## The problem

Trickcal has a big cast, and I can't hand model, rig, texture and animate every character on my own. The obvious shortcut is to feed the art into an image to 3D model. I tried that first and it didn't work. Flat, outlined chibi art has almost no depth cues, so every one shot tool gave me a paper thin cutout of the front view.

After a few of those, I started thinking of it as a missing information problem. A front view says nothing about the back of the hair or how far the wings stick out, and no model can guess that well. So instead of asking one tool to jump from one image to a finished character, I split the jump into small steps and generate each missing piece as an extra reference:

- The side and back views the art never showed.
- The character drawn onto my own shared base body, so the 3D result already matches my proportions, pose and rig.
- Each part (hair, horns, wings, tail) as its own mesh instead of one welded lump.
- A rough motion to edit, instead of waiting for a perfect one.

Each step is small enough for a tool to do well, and simple enough for me to check by eye before the next one runs.

I also set one rule early. AI generated meshes are too messy to rig, so only the rigid parts (hair, horns, ears, wings, tails) come from AI. Everything that bends, meaning the body, the head and the skeleton, is built once and shared by every character.

## The shared base body and rig

The base is a purchased mesh. Its license is royalty free but forbids sharing it publicly or using it to train AI, so the mesh itself stays in my private repository. I process it once, from the command line in headless Blender, with no manual modeling. The script strips what I don't need, cuts and caps the eye sockets and ears, rebuilds the face area as one smooth surface, and reshapes the head to the chibi look: cheek pushed forward and cranium shrunk so hair can cover it. Trickcal heads are short, so the head is also squashed to 0.8 of its height measured from the neck, and the hair to 0.9.

![The body after ear removal|full](/Dev-Showcase/media/VMP_base_ear_removal_body.webp)

![The head after ear removal|full](/Dev-Showcase/media/VMP_base_ear_removal_head.webp)

![The head stylized, with a test face painted on to check proportions|full](/Dev-Showcase/media/VMP_base_head_stylized.webp)

Then comes reduction to fixed triangle budgets, about 900 for the head, 2100 for the body and 6000 for a whole character. This was the first place I watched agents struggle. The first reduction scripts left the legs wobbly and dented, and the version that stuck brings the mesh down without tearing it, including at the armpits.

![Legs after one of the first reduction attempts|sm](/Dev-Showcase/media/VMP_h01_reduction.webp)

The rig script slices the body and finds each joint from the shape of the slices. The shoulder is where a slice holds only the arm, the wrist is where the thumb makes it deeper, and the ankle is where it gets shorter than the sole. It builds a Unity Humanoid compatible skeleton with extra bones for wings and tails, so every character shares one rig and one animation set.

![The auto rigged base body in its rest pose|full](/Dev-Showcase/media/VMP_rig_rest_pose.webp)

## Round 1 to 3, and what failed

I went through seven rounds in total, and every one is written in a decision log with what I tried, what happened and why I kept or dropped it. That log matters to me as much as the pipeline. It means I don't repeat an attempt, and the next character starts from evidence.

**One image to 3D.** Stable Fast 3D and TRELLIS.2 both gave paper thin cutouts. The tools weren't missing ability, they were missing information.

**Generate the views.** Gemini drew a side and back view from the front art, and TRELLIS.2 (running locally through ComfyUI) built from all three.

![Gemini's generated side view](/Dev-Showcase/media/VMP_ref_generated_side.webp)
![Gemini's generated back view](/Dev-Showcase/media/VMP_ref_generated_back.webp)

This was the first time I got real depth, and the whole pipeline grows from this result. The new problems were that the output is one welded mesh, which code can't cut into hair, body and clothing, and that it came out attached to a flat background plate.

![The multi view result next to my base body|full](/Dev-Showcase/media/VMP_trial_r2_welded_next_to_base.webp)

![The multi view result still attached to a background slab|full](/Dev-Showcase/media/VMP_trial_r2_welded_slab.webp)

**Generate the parts one by one.** I had Gemini draw my own base body in the character's outfit, so the output lined up with my rig with no fitting. The dressed views have no face. Faces get their own step, later.

![Kidian dressed onto the base, front](/Dev-Showcase/media/VMP_ref_dressed_front.webp)
![Kidian dressed onto the base, side](/Dev-Showcase/media/VMP_ref_dressed_side.webp)
![Kidian dressed onto the base, back](/Dev-Showcase/media/VMP_ref_dressed_back.webp)

Then SAM 3 cut each part out of each view and TRELLIS.2 built each part alone.

![The parts split out of each view in 2D|sm](/Dev-Showcase/media/VMP_trial_r3_split_2d.webp)

The parts generated alone came out as flat slabs. I also tried cutting the welded mesh from round 2 instead, by position and with masks projected from the 2D views. The cuts were wrong, and the hair came out jagged. Qwen-Image-Edit redrew the image when I asked it to erase a part. The base body idea was right and stayed. The splitting was the weak link.

![Cutting the welded mesh into parts by position, each color a different part|full](/Dev-Showcase/media/VMP_trial_r3_cut_fail.webp)

## Tripo, and the first mesh in game

Tripo takes multiple views and can return each part as its own low poly mesh. Fed the three dressed base views, it gave clean separate parts in one run, already sized to my base. Hunyuan3D is probably one of the better tools for this, but its license excludes South Korea, so I ruled it out. Tripo is a paid service, a cost I'm willing to take for how clean its output is.

This is the first Tripo mesh I tried in the game. The colors show how it split the character into parts.

![Tripo's first mesh in game, colored by part|sm](/Dev-Showcase/media/VMP_h00_tripo_start.webp)

## Texture trouble

Tripo had given me clean parts, but texture was another matter. I tried several routes, and most of the screenshots from this stretch are failures. MV-Adapter (on SDXL, running locally) repainted the whole character from the generated views onto my own UV layout. It took about 25 minutes per run and smeared wherever one part hid another, for example painting hair with the face color.

![A generated atlas as it landed in Unity|sm](/Dev-Showcase/media/VMP_h02_mv_mess.webp)

![The body with a generated texture|sm](/Dev-Showcase/media/VMP_h03_body_mess.webp)
![The same body from behind|sm](/Dev-Showcase/media/VMP_h04_body_back.webp)
![The hair from the side|sm](/Dev-Showcase/media/VMP_h05_hair_side.webp)

I dropped it for this, although it still looks fine as a local model for fast prototypes. The next idea was to take Tripo's meshes through my reduction step and have a local model generate new textures using Tripo's own texture and model as input. That didn't work either. Even after fixes to the texture pipeline, the ears were still wrecked. The cause turned up much later, and it is in the debugging list below.

![After pipeline fixes, the ears still broken|sm](/Dev-Showcase/media/VMP_h06_ears.webp)

## Trust the generator, adapt to the rig

What finally worked was giving up on beating Tripo at its own job. I kept Tripo's own meshes and textures and only fit, repair, bind and bake them into my format. Debug renders after every step showed that each step which replaced Tripo's output made it worse, including my own mesh reduction of thin hair. My code should adapt the AI output to the game, not compete with it.

![Tripo's own textured result on the shared base|sm](/Dev-Showcase/media/VMP_h07_tripo_texture.webp)

The fitting script works from a JSON recipe that says which piece is the hair, the horns or the wings, which bone it follows and how many triangles it gets. It sizes everything to the base and grows head parts around the larger base head. It adds a skin colored cap under the hair and pushes the hair off the skull so it doesn't clip. Pushing each point on its own tore the hair apart, so now each point moves by the largest push any nearby point needs, faded by distance. That makes the hair move as one smooth field.

![The first fit of Tripo's parts on the base, untextured|full](/Dev-Showcase/media/VMP_parts_first_fit.webp)

![The final fit with Tripo's textures|full](/Dev-Showcase/media/VMP_parts_final_fit.webp)

Every character ends up in one 1024 px atlas, so it costs one material and one draw call. The body is unwrapped into islands by the bone that moves them, and each part gets a tile of a 4 by 4 grid. Before the cleanup, the combined atlas looked like this:

![The combined texture before cleanup|sm](/Dev-Showcase/media/VMP_h08_combined_mess.webp)

## Debugging by looking

A lot of the work in the bake was debugging, and it got faster once I stopped guessing. Every step can write its own pictures, and I compare them side by side with Tripo's original render. A few examples:

- The bake picked up Tripo's normal map instead of its color map, which caused the strange colors. The fix was to read the image wired into Base Color.
- The fill color came out white, then grey. The first cause was a wrong median, and the second was sRGB against linear color.
- The arms came out black wherever Tripo's straight arms didn't line up with mine, which angle down. The fix was to fill the body with skin color before baking.
- Placeholder ears left on the base model caused the mess under the hair. The lesson was to check each step's pictures before blaming the step.

It still needed a human pass. In game, the pre cleanup character had patchy skin and white blotches.

![In game before cleanup, from behind|sm](/Dev-Showcase/media/VMP_h09_pre_cleanup_a.webp)
![In game before cleanup, from the side|sm](/Dev-Showcase/media/VMP_h10_pre_cleanup_b.webp)

I cleaned the texture up by hand. The flat map below is the result, and the character next to it is the same model in game afterward.

![The cleaned up flat map|sm](/Dev-Showcase/media/VMP_h12_flat_map.webp)
![In game after cleanup|sm](/Dev-Showcase/media/VMP_h11_post_cleanup.webp)

## The face

Faces are a separate texture, so the same body can switch expressions in game by moving the texture offset. For Kidian the first face was a generated image, which I polished and then baked.

![The first generated face](/Dev-Showcase/media/VMP_face_original.webp)
![The face after polishing](/Dev-Showcase/media/VMP_face_drawing_input.webp)
![The footprint of the face on the head's UV map](/Dev-Showcase/media/VMP_face_uv_guide.webp)

Getting it onto the head took a few placement trials with different sizes and positions. The head is squashed first, and the face is mapped onto it afterward at the same scale across and down, so the face keeps its proportions.

![A placement trial, large|full](/Dev-Showcase/media/VMP_face_trial_large_head.webp)

![A placement trial, small|full](/Dev-Showcase/media/VMP_face_trial_small_body.webp)

![The final face on the head|full](/Dev-Showcase/media/VMP_face_final_head.webp)

![The final face on the full character|full](/Dev-Showcase/media/VMP_face_final_full.webp)

And in the game:

![Final view, front](/Dev-Showcase/media/VMP_final_front.webp)
![Final view, angled](/Dev-Showcase/media/VMP_final_angled.webp)
![Final view, side](/Dev-Showcase/media/VMP_final_side.webp)
![Final view, back](/Dev-Showcase/media/VMP_final_back.webp)

## Animation

For motion I used UniMate, a text to motion research model, and Mixamo for common movement. UniMate runs locally on my RTX 4070, and the repository was set up for Linux, so getting it working on Windows took some fixes. It takes about 2 to 3 seconds per motion, and a batch of 24 takes baked to FBX in about 5 minutes. Each prompt gives a 2 second clip, and I picked the best take by eye in Unity.

UniMate only sees a skeleton, so a prompt can only describe body motion. I stripped out weapons, fingers and props, started every prompt with "An object", and chained actions with the sides and order stated plainly. The prompt for the kunai toss, for example, was "An object throws overhand forward with its right arm, then throws forward with its left arm."

Of the first six prompts, five gave a usable take, one of them by accident. I asked for a head bash, and the take I kept looked like a strong slash, so it became a skill. A couple of the usable ones needed cropping to a range of frames, like the kunai toss (frames 30 to 55) and the X slash (frames 43 to 59). I tried again for a proper head bash and a ninja run with six more prompts, and only one was usable after an edit. It was a plank prompt ("falls forward stiffly like a plank, bumps its head, and springs back up") that came out as a deep bow. A small script straightened the legs on a few frames, and it became the air dash.

![Combat stance, take 3 as generated, travel removed](/Dev-Showcase/media/VMP_anim_stance.mp4)
![Kunai toss, cropped to frames 30 to 55](/Dev-Showcase/media/VMP_anim_kunai_toss.mp4)
![Double slash, first second of one take](/Dev-Showcase/media/VMP_anim_double_slash.mp4)
![X slash, frames 43 to 59](/Dev-Showcase/media/VMP_anim_x_slash.mp4)
![Strong slash, from the head bash prompt, travel removed](/Dev-Showcase/media/VMP_anim_strong_slash.mp4)
![Air dash, legs straightened on a few frames](/Dev-Showcase/media/VMP_anim_air_dash.mp4)

I think the results are pretty good for early trials, with barely any knowledge of the UniMate project. The limits were clear too. Clips drift forward and back, which a script fixes on every take. Hands pass through the oversized chibi head, because UniMate sees bones and not the mesh. And rare poses fail: a proper ninja run, with both arms trailing and the legs alternating, never came out right in either batch.

That is where Mixamo came in. Running and jumping come from there instead, with the forward travel removed from the run so it plays in place. The jump is three Mixamo pieces: the jump up, the same clip reversed for the fall, and the last few frames played back and forth slowly to hang in the air. A retarget script bakes each clip directly onto my chibi rig, correcting for its different rest pose and smaller size. The split that came out of it is UniMate for unique skills, and Mixamo for common movement. A small clip tool crops, slows, reverses or loops anything that needs it.

![Run, from Mixamo](/Dev-Showcase/media/VMP_anim_run.mp4)
![Jump, from Mixamo](/Dev-Showcase/media/VMP_anim_jump.mp4)

UniMate's weights are non commercial, which is fine for a fan game.

## Where it stands

Kidian went from reference art to playable in game with her own model, texture atlas and ten animations: a stance, a run, three jump clips, four skills and an air dash. She comes out at about 6275 triangles with one texture atlas, which is cheap enough for a crowded MMO map. Every pipeline step is one command and writes review renders, a turntable sheet (front, three quarter, side, back) plus per step debug pictures, so I can approve or reject it by eye. That makes iterating on a character take minutes per step.

## The game under it

The game itself came first, and the pipeline feeds it. The server is C# on .NET 8 and fully authoritative. The client sends intent, and the server validates movement, cooldowns and damage. The Unity client and the server compile one shared library, so prediction and validation run the same movement and combat code. Each map instance runs its own 20 Hz tick, and maps with no players don't tick at all.

Messages go over WebSockets in a compact binary format using MemoryPack, with positions packed into 8 bytes. Persistence is SQLite, designed so every move of items or currency is a single transaction and can't duplicate an item. Basic protections are in from the start, including server side movement validation, flood control on every message and per IP connection limits.

I load tested the server with 500 simulated players that move and send like the real client. It came out at about 1.5 KB per second of server to client traffic per player, against a hosting budget of 10 to 20 USD a month. A single check script verifies localization, builds with warnings as errors, runs the tests and compiles the Unity client scripts outside the Editor.
