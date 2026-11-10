# UI styling

- _Theme.scss defines the app palette and semantic CSS variables, including aliases consumed by webapp-ui. Change theme values here, not inside panels.
- _Controls.scss defines opt-in appearance classes. Use ui-button (primary, ghost, chip, danger or warning variants), ui-item, ui-caption, ui-section-title, ui-muted, ui-badge and ui-empty.
- Keep panel classes alongside shared classes for layout, spacing and unique content. For example: className="tool-button ui-button" or className="save-button ui-button primary".
- Existing MVVM Class bindings add active, selected, current or armed. The shared styles render those states; no changes to view models are required.
- Panel SCSS owns layout and content-specific dimensions. Do not repeat control borders, backgrounds, typography or interaction states there. Add a shared variant when multiple controls need a new appearance.
- App.scss imports the framework theme first, then the app theme and controls. It also owns shell and popup integration. _Common.scss contains Sass helpers only; do not emit shared CSS from a module imported by every panel.
- Native fields share a low-specificity base so compound controls can override it. Framework Action components keep their behavior and consume the same theme tokens.
