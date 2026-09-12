# Outrun the Hollow — Unity version

A voice-controlled 2D infinite runner:

- **Talking/humming moves you forward.** Go quiet and the world (and you) slow to a stop.
- **A sudden shout/clap makes you jump** over obstacles.
- **The Hollow** is the NPC chasing you — represented as a shadow creeping in from
  the left. Run too little and it catches you → game over, restart.
- **Levels build themselves.** Obstacle variety and spacing are picked procedurally
  from a weighted, level-gated pool (`ObstacleSpawner.cs`), so it's a genuine
  endless runner, not a fixed set of hand-built levels.

The package includes an Editor scene builder that creates the scene, placeholder
visuals, UI, and obstacle prefabs automatically. Everything can also be wired
manually using the hierarchy below.

## 1. Requirements

- Unity 2021.3 LTS or newer (2D template recommended).
- **TextMeshPro** — Unity will prompt "Import TMP Essentials" the first time you
  use a `TMP_Text`/`TextMeshPro - Text` component; accept it.
- Legacy Input Manager active (Project Settings → Player → Active Input Handling
  = "Input Manager (Old)" or "Both") — only needed for the optional keyboard
  fallback script.

## 2. Install the scripts

Copy the whole `Assets/VoiceRunner/Scripts` folder from this package into your
project's `Assets` folder (keep the path `Assets/VoiceRunner/Scripts/...`).

After the scripts compile, use **Tools → Voice Runner → Create Demo Scene**.
This creates `Assets/VoiceRunnerUnity/VoiceRunnerUnity/Assets/VoiceRunner/Scenes/OutrunTheHollow.unity`, three generated
obstacle prefabs, and all Inspector references described below. Open that scene
and press Play. The generated scene uses simple colored placeholder sprites so
it is playable before custom art is added.

## 3. Tags

Select your future Player GameObject and set its **Tag** to `Player` (built-in
tag — no need to create it).

## 4. Scene hierarchy

Build this hierarchy (names matter for clarity, not function — wiring is via
Inspector references, not name lookups):

```
Managers
├── GameManager          (GameManager.cs)
├── MicrophoneInputManager (MicrophoneInputManager.cs)
└── KeyboardFallbackInput (optional, KeyboardFallbackInput.cs)

Main Camera              (orthographic, 2D)

World
├── Background
│   ├── HillsFar          (SpriteRenderer + ParallaxLayer.cs, speedMultiplier ~0.2)
│   └── HillsNear         (SpriteRenderer + ParallaxLayer.cs, speedMultiplier ~0.4)
├── Ground                (SpriteRenderer, static, wide tiling strip along the bottom)
├── HollowFog             (SpriteRenderer, pivot LEFT edge; parent, holds HollowChaser.cs)
│   └── FogVisual         (the actual left-anchored sprite that gets scaled on X)
├── Player
│   ├── Rigidbody2D (Body Type: Kinematic)
│   ├── Collider2D  (e.g. CircleCollider2D, NOT trigger)
│   ├── PlayerRunner.cs  (assign visualRoot -> its own child sprite, or leave empty)
│   └── Sprite/visual child (optional separate transform for the jump bob)
├── ObstacleSpawner        (ObstacleSpawner.cs)
└── ObstacleSpawnPoint     (empty Transform, positioned just off the right edge of the camera view)

Canvas (Screen Space - Overlay)
├── CanvasScaler (UI Scale Mode: Scale With Screen Size, Match: 0.5)
├── GraphicRaycaster
├── StartScreen (Panel)
│   ├── Title (TMP)
│   ├── MicButton (Button, "Start with microphone")
│   ├── NoMicButton (Button, "Play without a mic")
│   └── MicStatusText (TMP)
├── CalibratingScreen (Panel, starts inactive)
│   └── CalibratingText (TMP, e.g. "Stay quiet a second…")
├── HUD (starts inactive)
│   ├── ScoreText (TMP)
│   ├── VolumeMeter (Image, Filled/Horizontal) -> volumeMeterFill
│   └── LevelToast (CanvasGroup + TMP child, alpha 0 by default)
├── FallbackControls (starts inactive)
│   ├── HoldToTalkButton (Button + HoldToTalkButton.cs, "Hold to run")
│   └── ShoutButton (Button, "Shout / Jump")
└── GameOverScreen (Panel, starts inactive)
    ├── FinalScoreText (TMP)
    ├── FinalLevelText (TMP)
    └── RetryButton (Button, "Run again")

UI/GameFlow
└── UIManager           (UIManager.cs)
```

- Exactly **one EventSystem** must exist in the scene (Unity adds it automatically
  the first time you create a Canvas — don't add a second one).
- Every screen except `StartScreen` should be **inactive by default** in the
  hierarchy; `UIManager.Awake()` also force-disables them, but starting them
  correctly avoids a flash on load.

## 5. Wire up `UIManager`

Drag each object above into the matching field in the `UIManager` Inspector
(Screens, Start Screen, HUD, Game Over, Fallback Controls, Level → ObstacleSpawner).

## 6. Wire up `PlayerRunner`

- `visualRoot`: drag the child sprite transform that should bob on jump (or
  leave empty to move the Player object itself).
- If you add a simple run-cycle Animator, assign it and it'll receive a
  `Speed01` float (0–1) each frame — otherwise leave `animator` empty.

## 7. Wire up `HollowChaser`

- `fogVisual`: the child sprite whose X scale represents how far the Hollow
  has crept in. Pivot must be at its **left edge** (Sprite import settings →
  Pivot → Left, or offset the mesh) so scaling grows it rightward from x=0.
- `fogRenderer`: the same sprite's `SpriteRenderer`, for the safe→urgent color lerp.
- `playerX`: set to your Player's world-space X position.

## 8. Build the obstacle pool

Create 2–3 simple obstacle prefabs (a colored sprite + `BoxCollider2D` set to
**Is Trigger** + `Obstacle.cs`), e.g.:

| Prefab | Suggested unlockLevel |
|---|---|
| Spike (small) | 1 |
| Double spike (wide) | 2 |
| Wall (tall block) | 4 |

Drag each into `ObstacleSpawner`'s `Obstacle Pool` list with an `unlockLevel`
and `weight`. Set `Spawn Point` to `ObstacleSpawnPoint` from the hierarchy above.

## 9. Microphone permissions (mobile builds)

- **iOS**: Project Settings → Player → iOS → Other Settings → set
  "Microphone Usage Description" to something like "Used to control your
  character by voice."
- **Android**: the `RECORD_AUDIO` permission is added automatically because
  the project uses the `Microphone` API; `MicrophoneInputManager` also calls
  `Application.RequestUserAuthorization` at runtime on both platforms.
- Desktop/Editor: no special permission step, the OS mic-access prompt (if any)
  is handled by the OS itself.

## 10. Tuning knobs

All in `GameManager`'s Inspector:

- `baseMaxSpeed` / `speedLevelGrowth` — how fast the world moves and how much
  faster it gets per level.
- `safeSpeedFraction` — the speed threshold (as a fraction of max) above which
  the Hollow recedes instead of advancing.
- `hollowRecoveryRate` — how fast the chase meter reacts to being above/below
  safe speed. Higher = more punishing.
- `collisionPenalty` / `collisionInvulnDuration` — cost and grace period for
  clipping an obstacle.

In `ObstacleSpawner`: `baseGapRangeMeters`, `gapShrinkPerLevel`, `minGapMeters`
control how the procedurally generated track gets denser over time.

## 11. Play

Press Play, click **Start with microphone**, stay quiet for the ~1 second
calibration, then talk/hum to run and shout to jump. Use **Play without a mic**
(or hold Space / press Up-Arrow in the Editor) to test without a working
microphone.
