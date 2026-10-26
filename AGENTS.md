# AGENTS.md

## Rules for this file

- This file is for AI coding agents only. Keep all content in English.
- Include only information needed for fast orientation and navigation — no fluff, no human-facing docs.

## Project Overview

**voting-sim** — Unity educational voting-procedure simulator (first-person, greybox → MVP).

This repo currently holds **seed config only**. The Unity project lives with another contributor; they will pull these files and push the full project here. If you are AI-agent and you pushing this repository, then delete this statement before push.

## Repo layout

| Path              | Role                                                |
| ----------------- | --------------------------------------------------- |
| `.gitignore`      | Unity standard ignore (Library, Temp, Builds, etc.) |
| `.gitattributes`  | Unity YAML merge + Git LFS for binary assets        |
| `docs/ROADMAP.md` | Product roadmap (Russian; human planning doc)       |
| `AGENTS.md`       | This file                                           |

Expect a normal Unity root once the project lands: `Assets/`, `Packages/`, `ProjectSettings/`.

## Git / LFS (important)

- **Not a git repo yet** in this folder until `git init` / clone is done on the machine that owns the Unity project.
- `.gitattributes`: text/diff for C#/shaders; `merge=unityyamlmerge` + `eol=lf` for Unity YAML (`.mat`, `.prefab`, `.unity`, `.meta`, etc.); **Git LFS** for images, audio, video, 3D, fonts, `.dll`, `.unitypackage`, etc.
- Agents must not commit ignored Unity caches (`Library/`, `Temp/`, `Logs/`, `UserSettings/`, etc.).

## Product context (for agents)

Roadmap lives in `docs/ROADMAP.md`. Short map:

1. **Stage 0** — repo, Unity 6, folder structure
2. **Stage 1** — FPS walk + interact (E), greybox polling station
3. **Stage 2** — voting MVP (register → ballot → validate → ballot box)
4. **Stage 3** — on-screen guidance + end feedback
5. **Stage 4** — menu/pause/settings, candidates, Windows build → full playthrough MVP

## Agent workflow notes

- Prefer English in code, comments, commit messages, and agent docs.
- Do not invent Unity project structure here until the real project is pushed; edit seed configs carefully so they remain merge-friendly for the incoming Unity tree.
- Human roadmap is Russian; when aligning work with it, translate intent — do not rewrite `docs/ROADMAP.md` unless asked.
