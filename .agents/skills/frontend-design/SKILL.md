---
name: frontend-design
description: "Use when designing or implementing frontend interfaces in BriefForge. Routes visual design, UX, accessibility, responsive behavior, typography, color, motion, and UI polish through the installed impeccable skill."
---

# Frontend Design

This project-local skill is a compatibility entry point for frontend design work.

For substantial visual work, follow the installed `impeccable` skill at `.agents/skills/impeccable/SKILL.md`, including its context-loading, reference, craft-floor, and visual validation workflow.

## Project requirements

- Inspect the existing page, components, styles, assets, and dependencies before editing.
- Preserve the current product behavior and user changes unless the request explicitly asks for a redesign.
- Use the existing React, TypeScript, Vite, Tailwind CSS, and accessibility conventions.
- Prefer existing components and tokens over adding a second styling system or unnecessary dependencies.
- Check mobile and desktop layouts, focus states, loading states, empty states, errors, and reduced-motion behavior.
- Do not edit `dist/` or `node_modules/`.

## Validation

Run the narrowest relevant check, then use `npm run build` and `npm run lint` for shared or user-facing changes. Report the changed files and validation results.

The upstream `getclaudeskills/frontend-design` repository was not reachable during setup, so this file is a local routing skill rather than a claimed copy of that remote package.
