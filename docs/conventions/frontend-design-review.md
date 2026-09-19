# Frontend Design Review Gate

Run this gate after any UI slice changes visible application screens.

## Inputs

- `DESIGN.md`
- Changed `.html`, `.css`, and component/page `.ts` files
- Nearest shipped screen or test fixture when available

## Deterministic checks

A UI slice is not review-ready until all applicable checks pass or an explicit exception is recorded in the PR body.

### Design-system alignment

- Uses Lorekeeper semantic tokens or direct values from `DESIGN.md` for parchment, ink, pimentón, border, and muted text colors.
- Preserves the Codex Lithographica direction: light parchment surface, serif editorial/display hierarchy, mono metadata/action styling, and pimentón accents.
- Avoids generic SaaS/dashboard styling such as gray corporate panels, blue primary actions, green success defaults, or hidden disabled future controls.
- Keeps templates and styles in separate `.html` and `.css` files; no inline page templates for routable UI.

### UX states

- Covers loading, empty, ready/success, validation, conflict, recoverable error, and retry states when the feature owns those states.
- Does not imply unavailable future features are usable.
- Does not silently select campaign context or use stored active-campaign preferences unless the product spec explicitly says so.

### Accessibility and responsive behavior

- Uses semantic headings and labels.
- Associates field errors with the relevant input.
- Provides visible focus.
- Keeps navigation and actions keyboard reachable.
- Reflows in normal document flow without horizontal page overflow at narrow widths.

### Evidence

Record the result in the PR summary or SDD apply-progress with:

```text
Design review: pass|exception
Checked against: DESIGN.md, docs/conventions/frontend-design-review.md
Notes: <short notes or exceptions>
```
