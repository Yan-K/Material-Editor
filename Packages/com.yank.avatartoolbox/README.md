# Yan-K Avatar Toolbox (YAT)

A collection of Unity editor tools for streamlining VRChat avatar authoring.

> This repository was previously **Yan-K Material Editor (YME)** and has been renamed to **Yan-K Avatar Toolbox (YAT)** to reflect its expanded scope.

<table>
  <tr>
    <td><img src="demo1.png" width="400"></td>
    <td><img src="demo2.png" width="400"></td>
  </tr>
  <tr>
    <td><img src="demo3.png" width="400"></td>
    <td><img src="demo4.png" width="400"></td>
  </tr>
</table>

## Tools Included

### Yan-K Material Editor (YME)

Edit Materials and Textures in bulk.

- **Bulk Material & Texture Management** — List, replace, clone, and reset materials and textures across all child renderers
- **Search & Filter** — Quickly find materials by name or shader, and textures by name or property
- **Batch Operations** — Select multiple items and clone, replace, or reset them all at once
- **Modified Indicator** — Visual highlight on items that have been changed from their original
- **Include Inactive** — Optionally scan inactive GameObjects, with persistent toggle across sessions
- **Confirmation Dialogs** — Destructive batch resets require confirmation to prevent accidents
- **Undo Support** — All operations are fully undoable

### Yan-K Blendshape Editor (YBE)

Drive 600+ blendshapes without losing your mind. 

- **Auto Group Detection** — Parses separator rows like `===Eye===` / `---Mouth---` into wrapping, clickable group tabs
- **Search & Filter** — Instantly narrow down a long blendshape list by substring
- **Batch Value Slider** — Real-time drive every selected blendshape with one slider, collapsed into a single Undo step
- **Shift-Click Range Select** — Explorer-style range selection on the row checkboxes
- **Reset to Zero / Default** — One-click reset with confirmation
- **Export as AnimationClip** — Export all / non-zero / custom-selected blendshapes to a reusable `.anim`
- **Import AnimationClip (4 modes)** — Overlay / Reset Zero / Reset Default / Custom, with live preview before committing
- **Remap Missing Blendshapes** — Searchable, grouped dropdown plus fuzzy-name auto-match for clips authored on a different avatar
- **Undo Support** — Every commit registers proper Undo

### Shared

- **Localization** — English, 简体中文, 繁體中文, 日本語, 한국어
- **Theme Aware** — Adapts to both dark and light editor themes

## Installation

- Add to VCC via [VPM Listing from Explosive Theorem Lab.](https://xtlcdn.github.io/vpm/).
- Download .unitypackage from [Release](https://github.com/Yan-K/AvatarToolbox/releases) and import to Unity.

## Changelog

### v0.1.0 - 2024/11/27

Inital Release.

### v0.2.0 - 2026/04/06

Added Clone, Reset, Batch Selection, Renderer Foldout.

### v0.3.0 - 2026/04/07

Added Texture Mode.

### v0.3.1 - 2026/04/10

Added Total Number for Materials and Textures.

### v0.4.0 - 2026/04/10

UX Overhaul.

### v0.4.1 - 2026/04/10

Fixed suffix in clone.

### v0.4.2 - 2026/04/11

Fixed modified list card style.

### v1.0.0 - 2026/04/11

UI/UX Unified, overall cleanup, changed language format.

### v1.1.0 - 2026/04/22

Repository renamed from **Yan-K Material Editor** to **Yan-K Avatar Toolbox**. Added **Yan-K Blendshape Editor (YBE)**: group auto-detection, search/filter, real-time batch slider, shift-click range select, reset-to-zero/default, AnimationClip export (3 modes), AnimationClip import with live preview (4 modes), searchable grouped remap dropdown with fuzzy-name auto-match, and full 5-language localization.

## Credit

- Yan-K ([@YanKMW](https://github.com/Yan-K))
- Vistanz ([@JLChnToZ](https://github.com/JLChnToZ)) for VPM Listing
