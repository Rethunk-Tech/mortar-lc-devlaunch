# Changelog

## [Unreleased]

### Fixed

- The corner label sizes itself with decimal maths, so the Host or Client N badge scales smoothly on every screen size.
- Dependabot's NuGet update checks no longer fail trying to reach Thunderstore.

## 0.1.1

- The host no longer sticks at the LAN warning when a client copy is writing the game's settings file at the same
  moment: a copy waits and tries again.
- Square icon background.

## 0.1.0

- First release. `--lan` or `--online`, `--host` and `--join <address>` skip the splash screen, the logo intro, the menus and the LAN warning.
- `--tile <n>/<count>` lays copies out in a grid, and each copy shows Host or Client N in a corner and its window title,
  with settings for where and how.
