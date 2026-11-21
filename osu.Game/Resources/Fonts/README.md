# scosu's fonts

They replace osu!'s Torus, Torus Alternate and Venera, which ppy's licence doesn't let other projects ship.

| Folder      | Font                                                     | Replaces                 | Weights                        |
|-------------|----------------------------------------------------------|--------------------------|--------------------------------|
| `Outfit`    | [Outfit](https://fonts.google.com/specimen/Outfit)       | Torus and Torus Alternate | Light, Regular, SemiBold, Bold |
| `Unbounded` | [Unbounded](https://fonts.google.com/specimen/Unbounded) | Venera                   | Light, Bold, Black             |

Both are under the SIL Open Font License 1.1 (`OFL.txt` in each folder).

The files are osu-framework glyph stores (AngelCode BMFont binary `.bin` plus `.png` pages), made from the
variable fonts of github.com/google/fonts with the settings ppy used for its own fonts (line height 100px,
smoothing, 4px spacing, 1024x1024 pages, kerning). Each set is sized so a capital H is as tall as in the font
it replaces, on the same baseline, so layouts made for Torus and Venera still fit:

- Outfit: H 56px, baseline 84 (as Torus); Unbounded: H 69px, baseline 75 (as Venera).
- Characters: the ones Torus and Venera had (Latin, punctuation, symbols); other scripts fall back to Noto as before.
