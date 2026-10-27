# AGENTS.md

## Rules for this file

- This file is for AI coding agents only. Keep all content in English.
- Include only information needed for fast orientation and navigation — no fluff, no human-facing docs.

## Project Overview

**voting-sim** — Unity educational voting-procedure simulator (first-person, greybox → MVP).

| Item   | Value                                                 |
| ------ | ----------------------------------------------------- |
| Engine | Unity **6000.6.6f1**                                  |
| Render | URP (`com.unity.render-pipelines.universal` 17.6)     |
| Input  | New Input System (`Assets/InputActions.inputactions`) |
| UI     | uGUI + TextMesh Pro (core only; no Examples & Extras) |
| Target | Windows desktop MVP                                   |

## Repo layout

| Path                                                      | Role                                                                |
| --------------------------------------------------------- | ------------------------------------------------------------------- |
| `Assets/Scripts/`                                         | Gameplay C# (player, interact, ballot, inventory, pause)            |
| `Assets/Scripts/PlayerController/`                        | Movement / look / interaction / inventory handlers                  |
| `Assets/Scripts/InteractableObjects/`                     | World interactables (e.g. ballot)                                   |
| `Assets/Scenes/SampleScene.unity`                         | Dev/test sandbox — keep; do not replace with the polling station    |
| `Assets/Scenes/PollingStation.unity`                      | Sports-hall greybox 12×24×6 (specs); windows W, doors E             |
| `Assets/Prefabs/Player.prefab`                            | FPS player extracted from SampleScene                               |
| `Assets/Prefabs/PlayerHUD.prefab`                         | Canvas (pause / inventory / prompts)                                |
| `Assets/Settings/`                                        | URP PC/Mobile assets + volume profile                               |
| `Assets/QuickOutline/`                                    | Outline plugin (Scripts + Resources only)                           |
| `Assets/Art/Interior/PollingStation/`                     | Sports-hall polling station art (Models/Materials/Textures/Prefabs) |
| `Assets/Art/Interior/Home/`                               | Home location art (same subfolders)                                 |
| `Assets/Art/Exterior/<Location>/`                         | Exterior per location (e.g. `Street/`)                              |
| `Assets/Art/Shared/`                                      | Shared materials & textures                                         |
| `Assets/Materials/`, `Prefabs/`, `Sprites/`, `Inventory/` | Greybox mats, UI prefabs, test sprites/items                        |
| `Packages/`, `ProjectSettings/`                           | UPM + Unity project settings                                        |
| `docs/ROADMAP.md`                                         | Product roadmap (Russian; human planning)                           |
| `.cursor/rules/`                                          | Cursor agent rules (skills routing, conventions)                    |
| `.gitignore` / `.gitattributes`                           | Unity ignore + YAML merge + Git LFS                                 |

## Current gameplay state

Implemented (prototype in `SampleScene`): FPS walk/look/sprint/jump; raycast interact (E); grab/throw/drop; inventory hotbar; ballot draw UI; pause menu; QuickOutline highlights.

Not done yet (roadmap Stage 1–2+): furnish polling station (desk/booth/urn); registration desk flow; ballot validation; ballot box; on-screen guidance; main menu; candidates; Windows build polish.

Greybox room scene exists: `PollingStation.unity` (rebuild via menu **Voting Sim → Build Polling Station Greybox Scene** or `PollingStationSceneBuilder.Build`).

## Required skills (read before related work)

Agent skills live under the user's installed skills roots (e.g. `.agents/skills/`). **Read the matching `SKILL.md` before implementing.** Cursor rules in `.cursor/rules/` reinforce this.

| When                                  | Skill name                                         |
| ------------------------------------- | -------------------------------------------------- |
| Any Unity `.cs` / MonoBehaviour       | `unity-csharp-scripting`                           |
| Input / `.inputactions` / actions     | `unity-input-system`                               |
| UI without named framework            | `ui` (then route)                                  |
| Canvas / RectTransform / uGUI         | `ui-ugui`                                          |
| Colliders, triggers, raycasts miss    | `physics-3d-collision`                             |
| Drive Editor / scenes / batch / tests | `unity-cli` (+ `unity-pipeline` if Editor is open) |
| URP Volume / post-processing          | `urp-postprocessing`                               |
| Add/remove/upgrade UPM packages       | `unity-package-management`                         |
| TMP fonts / atlas / performance       | `optimize-text-mesh-pro`                           |
| Project Auditor CSV fixes             | `project-auditor-fixes`                            |

Do **not** pull unrelated stacks (MAUI, Blazor, web, etc.) into this repo.

## Setup / open project

- Open folder in Unity Hub with Editor **6000.6.6f1** (or matching 6000.6.x).
- First open generates `Library/` (gitignored). Wait for import + script compile.
- CLI (if installed): `unity` / Unity Editor batchmode against this project path.
- Do not commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, Builds.

## Development workflow

- Prefer English in code, comments, commit messages, and agent docs.
- `SampleScene` = tests/sandbox. New playable space (polling station greybox) = **new scene** under `Assets/Scenes/`; add to Build Settings when ready; do not overwrite SampleScene.
- Keep `.meta` files with their assets; never hand-delete `.meta` without the asset (or vice versa).
- Prefer editing scenes/prefabs via Unity Editor or `unity-cli` / pipeline when available — avoid huge blind YAML edits unless necessary.
- UPM: use Package Manager / `unity-package-management` skill — avoid casual hand-edits to `Packages/manifest.json`.
- Roadmap is Russian in `docs/ROADMAP.md`; translate intent when aligning work — do not rewrite it unless asked.

## Code style

- C# / Unity idioms per `unity-csharp-scripting` (lifecycle, `[SerializeField]`, no heavy `Find` in `Update`).
- New scripts under `Assets/Scripts/` (or a clear subfolder); match existing handler split under `PlayerController/`.
- uGUI for runtime UI unless a decision is made to migrate.
- New Input System only — do not reintroduce legacy `Input.GetAxis` / `Input.GetKey` for gameplay.
- **Encoding:** all text sources UTF-8 (see root `.editorconfig` `charset = utf-8`). Never save `.cs` as Windows-1251 — Cyrillic comments break on GitHub and other machines.

## Testing

- Manual play in Editor (SampleScene / future station scene).
- `com.unity.test-framework` is present; add EditMode/PlayMode tests under `Assets/` when introducing non-trivial logic (ballot validity, voter state).
- With Editor + pipeline: `unity command` test runners per `unity-cli` / `unity-pipeline`.

## Build

- MVP target: Windows standalone (roadmap Stage 4).
- Use Unity Build Settings / CLI build — do not commit build output folders.

## Git / LFS

- Branch work away from `main` for features (e.g. greybox station).
- **Commits on `main`:** write commit messages in **Russian**.
- **Merging a feature branch into `main`:** use a merge commit whose message **summarizes in Russian** what the branch adds (not a bare "Merge branch …").
- Feature-branch commits may stay in English.
- `.gitattributes`: text/diff for C#/shaders; `merge=unityyamlmerge` + `eol=lf` for Unity YAML; Git LFS for images, audio, video, 3D, fonts, `.dll`, `.unitypackage`, etc. Also LFS for `.blend` / `.fbx` / `.glb`.
- Never commit secrets or local Editor caches.

## Cleanup policy

Already removed: URP `TutorialInfo`, root `Readme.asset`, TMP `Examples & Extras`, QuickOutline `Samples`.
Keep unless asked otherwise: test sprites (`Коля`, `Серая сфера`), `Sphere` prefab, `NewItemData`, TMP core (`Fonts` / `Resources` / `Shaders`), QuickOutline Scripts + Resources.
