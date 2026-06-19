# AI Bully — Narrative & Bully System

> An LLM-driven "bully" AI plus a scripted intro, for the 2026 Game AI Summer School Jam.
> A local LLM (LLMUnity / llama.cpp) generates the bully's taunts in reaction to game state;
> Yarn Spinner runs the opening DAY 0–5 narrative. No cloud, no API.

This file documents the narrative/bully module only. It lives in `Assets/Bully_AI/Narrative/`
and is developed on the `narrative` branch.

## Stack
- **Unity** 6.0.40
- **LLMUnity** ("LLM for Unity") — local LLM, in-engine, via llama.cpp
- **Yarn Spinner** — the intro dialogue
- **ML-Agents** — the RL agent for gameplay (the bully reacts to its actions + the player's state)

## Flow (start → gameplay)
```
Scene loads
  → GameFlow hides the bully/gameplay UI
  → Yarn intro auto-starts
DAY 0 → DAY 1 → ... → DAY 5      (each: one line + one button: NEXT / FIX IT / STOP AGENT)
  → DAY 5 "STOP AGENT" runs  <<startGameplay>>
  → GameFlow reveals the bully UI → GAMEPLAY begins
Gameplay:
  game event → build a BullyContext → BullyBrain.React()
            → prompt → local LLM → taunt → shown
  (right now the 3 BullyTestDriver buttons stand in for real game events)
```

## Scripts

### In use
- **BullyContext.cs** — *data only.* A packet describing what just happened to the player
  (fields + a `Describe()` method that flattens them into a sentence for the model). This is
  the single "seam" the game fills in and hands to the bully.
- **BullyBrain.cs** — *the core / translator.* `React(BullyContext)` builds a prompt
  (`BuildPrompt` + `Describe`), calls the LLM (`LLMAgent.Chat`), streams the reply, applies
  fallback lines, and publishes the result via `OnTaunt` / `OnTauntStreaming`. Warms up the
  model on `Start`. `React` is async with a `finally` that always frees `IsBusy`, so it can't
  get stuck. The bully's personality + sampling live on the **LLMAgent** component, not here.
- **BullyTestDriver.cs** — *dev/sandbox only.* Three OnGUI buttons that build fake
  `BullyContext`s and call `React()`, plus an on-screen readout. Stands in for real gameplay
  events; will be removed once real triggers exist.
- **GameFlow.cs** — *intro → gameplay handoff.* Hides the bully/gameplay UI during the intro
  (`Awake`), and reveals it when the Yarn intro runs `<<startGameplay>>`. It registers that
  command with `dialogueRunner.AddCommandHandler("startGameplay", ...)`. Keep the LLM + BullyAgent
  OUT of its hidden list so the model can warm up during the intro.
- **Intro.yarn** (+ `IntroDialogue.yarnproject`) — the DAY 0–5 intro. Each DAY is a node: one
  line of text + a single option (the option text is the button label) that `<<jump>>`s to the
  next day. Linear, no real branching. The Dialogue System prefab (Dialogue Runner + Line/Options
  Presenter) renders it — no custom UI code needed.

### Optional (built, not currently wired in)
- **SpeechBubble.cs** — shows the taunt in a UI bubble that tracks the bully's head on screen
  (subscribes to `OnTaunt`, positions via `Camera.WorldToScreenPoint`).
- **BullyVoice.cs** — speaks the taunt with the OS voice (macOS `say` / Windows SAPI), fired off
  `OnTaunt` so the voice and the on-screen text happen together.

Both attach to their own GameObjects and only need `OnTaunt`, so they can be dropped in/out
without touching `BullyBrain`.

## Key settings (tuned — don't regress)
- **Num Predict = 50** on the LLMAgent (NOT `-1`: `-1` → runaway generation, slow + stuck-after-one).
- **Seed = -1** (a fixed seed → the same taunt every time).
- **Temperature ≈ 0.8** (0 → deterministic/repetitive).
- **Small model** (1–3B, Q4). The `.gguf` is NOT committed — each machine downloads it via
  LLMUnity's model manager.
- Packages (Yarn Spinner + LLMUnity) are in `Packages/manifest.json` → auto-restore on project open.

## Next steps

### 1. Player representation / variables (the bully's "observation")
`BullyContext` is effectively the bully's hand-crafted observation of the player. Make it richer,
and let the bully's *tone* be derived from it:
- **Add fields** to `BullyContext`: `winStreak`, `lossStreak`, `recentDeaths`, `idleSeconds`,
  `repeatedSameMistake`, `frustration` (0–1), `tauntCount`. (Add a field only if it would change
  what the bully would say.)
- **Add `DeriveMood(context)`** in `BullyBrain` that maps the state to a tone directive — e.g.
  long loss streak → gleeful/savage; player on a win streak → rattled/defensive; idle → goading.
  This is the mechanism that makes the player's situation change the bully's character.
- **Populate the fields from real systems**: score manager, round manager, player controller,
  and the ML-Agents agent (what it did / its outcome).
- **Advanced**: feed the RL agent's own signals — its value estimate, or a hidden-layer embedding
  — into the context as a genuine *learned* latent, instead of only hand-crafted features.

### 2. Prompt composition (what actually shapes the bully)
The prompt the model sees is assembled from distinct parts. Tune each separately:
- **System Prompt (persona)** — set on the LLMAgent. Defines WHO the bully is: tone, vocabulary,
  hard rules ("one short line", "never break character"). The biggest lever on character.
- **Game-state context** — `BullyContext.Describe()`: a compact sentence of what just happened.
  Grounds the taunt in real facts so it isn't generic.
- **Mood / tone directive** — from `DeriveMood()`: shifts the attitude per turn based on the
  player's state (the link between representation and character).
- **Per-turn instruction** — `maxWords`, "output only the line", format. Controls shape and length.
- **(Later) Grammar / structured output** — constrain output to `{line, emotion, intensity}`
  (LLMUnity's grammar field) to drive animation / voice.

### 3. Replace the test buttons with real gameplay
Wire real events (a round ends, the RL agent acts, the player fails) to build a `BullyContext`
and call `BullyBrain.React()`, then delete `BullyTestDriver`.

### 4. (Optional) Presentation
Re-add `SpeechBubble` (in-world text) and/or `BullyVoice` (OS voice) by subscribing to `OnTaunt`.

## Status
- [x] Local LLM taunts working (triggered by the 3 test buttons), output tuned
- [x] Yarn intro DAY 0–5 (linear, single-button beats)
- [x] Intro → gameplay handoff (`GameFlow` hides/reveals the bully UI via `<<startGameplay>>`)
- [ ] Richer player representation + `DeriveMood` (next step 1)
- [ ] Real gameplay triggers replacing `BullyTestDriver` (next step 3)
- [ ] Learned latent from the RL agent (next step 1, advanced)
- [ ] Re-wire voice / bubble (next step 4)

## Where this lives & how to work on it
- All narrative/bully code: `Assets/Bully_AI/Narrative/` (`Scripts/` + `Yarn/`).
- Branch: `narrative`. Merge into `release_23` via a Pull Request when ready (let Carlos review).
- The model is not in git — download it via LLMUnity's model manager after pulling.
