# UI reference analysis - secret.fix v0.2

## Source files

- Reference video found in the repository after fast-forwarding `main`: `docs/reference/precisionfix-v3.mp4.mp4`.
- Intended final path: `docs/reference/precisionfix-v3.mp4`.
- Shell metadata reports duration `00:01:28`; local `ffmpeg`/`ffprobe` are not installed and no Python video decoder is available, so frame extraction could not be performed locally.
- Background video found in the repository after fast-forwarding `main`: `src/SecretFix.App/Assets/Backgrounds/red-galaxy.mp4.mp4`.
- Intended final path: `src/SecretFix.App/Assets/Backgrounds/red-galaxy.mp4`.
- Shell metadata reports background duration `00:01:02`; file size is about `22.4 MB`.

## Visual observations

The approved reference screenshots show a compact desktop utility, not a web dashboard. The window is wide but controlled, with a fixed left sidebar around 250-280 px and a content area that uses the remaining width. The main feeling is quiet, black, dense, and premium. Red is used for selected states, active toggles, narrow accents, progress, and notifications, not as a large surface color.

The sidebar has the product name at the top, a small version label, an optimization progress card, vertical navigation, and a user card pinned to the bottom. Navigation rows are compact, icon-first, mostly transparent in the normal state, and only gain a darker panel plus subtle red border when selected. Hover states should be short and restrained.

The Mouse page uses a three-part composition: options on the left, a large central device image, and system options on the right. The mouse must dominate the center, with enough vertical scale to feel like a product-focused utility. Notifications stack in the top-right. The device selector is a horizontal row at the bottom, with each card containing image, brand, and model entirely within the card.

The Keyboard page mirrors Mouse, but the central keyboard is wide and large. Left and right option groups are visually lighter than full dashboards; the reference often uses simple text rows and compact checkboxes. Keyboard cards are wider than mouse cards, with the image taking the upper half and text safely contained below.

Page density is high but not cramped. Headers use 20-24 px titles and smaller gray descriptions. Cards have fine borders, low-radius corners, dark surfaces, and minimal shadows. The overall layout should avoid giant gaps, oversized buttons, and generic KPI panels.

Animations should be fast: splash around one second, page transitions under 250 ms, hover/select transitions around 120-180 ms, and device-card lift/scale around 150-200 ms. The effect should make the app feel alive without turning into a neon/gamer interface.

## Adaptation for secret.fix

- Keep the `secret.fix` identity with black/red colors and no PrecisionFix branding.
- Use `red-galaxy.mp4` as a subtle background behind a dark overlay.
- Keep sidebar/cards highly opaque so text remains readable.
- Preserve backend services and only connect real actions where existing safe Windows services support them.
- Mark unsupported or not-yet-safe actions as visual, locked, experimental, or in development.
- Preserve CORE/PULSE/APEX through `FeatureCatalog`; blocked features remain visible.
