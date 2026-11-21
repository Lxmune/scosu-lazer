# scosu's resources

Everything here is embedded into osu.Game.dll (`Directory.Build.props`: `Resources\**\*.*`) and is read before osu!'s
own resources (the `ppy.osu.Game.Resources` package; see `OsuGameBase`). A file here with the same path as one of osu!'s
replaces it: `Textures/Menu/menu-background-1.jpg` here is shown instead of osu!'s. scosu replaces osu!'s branding, the
fonts ppy licenses from type foundries and the music by outside artists; osu!'s other resources (UI and gameplay sounds,
skins) are kept under their own licence, which asks for credit to ppy and non-commercial use.

Every file here must be ours to ship: made for scosu, or under a licence that allows it (CC0, OFL...). List each source
and licence below.

## Fonts

See `Fonts/README.md`.

## Sources and licences

| Path | What | Source | Licence |
|------|------|--------|---------|
| `Textures/Menu/scosu-logo.png` | main menu logo | made for scosu | scosu |
| `Fonts/Outfit`, `Fonts/Unbounded` | fonts | Google Fonts | OFL 1.1 |
| `Textures/Icons/Logo.png`, `RulesetOsu.png`, `RulesetTaiko.png`, `RulesetCatch.png`, `RulesetMania.png` (90x90), `play-circle.png` (100x100, the main menu's play button) | icon-font glyphs for the logo mark and the four ruleset icons: ring with an S, hit circle, split drum, three fruits, key bars (standard and catch after osu!'s own layout, with their own proportions) | drawn for scosu with Python/Pillow (supersampled geometric primitives); script kept outside the repo | scosu (original) |
| `Textures/Menu/menu-background-1..8.jpg`, `Textures/Intro/Welcome/menu-background.jpg` | 9 menu backgrounds: violet mountains, aurora, gradient mesh, bokeh, low-poly, dusk dunes, moonlit mountains, rings, nebula | procedurally generated for scosu with Python (numpy/scipy/Pillow) | scosu (original) |
| `Textures/Intro/Triangles/logo-background.png`, `logo-highlight.png` | outline-draw animation maps for the seasonal intro logo (R = reveal progress, G = alpha): double ring and "SCOSU" lettering | generated for scosu with Python (geometric letters, distance-transform outlines) | scosu (original) |
| `Textures/Menu/hat.png`, `Textures/Menu/logo.png`, `Textures/Menu/dev-build-footer.png` | Santa hat for the logo, copy of the scosu logo, dev-build hazard stripe | drawn / copied for scosu (Python/Pillow) | scosu (original) |
| `Textures/Online/avatar-guest.png`, `not-found.png`, `supporter-heart.png`, `supporter-pippi.png`, `supporter-required.png` | guest avatar silhouette, magnifier "not found", heart badge, big heart, padlock | drawn for scosu with Python/Pillow | scosu (original) |
| `Textures/Backgrounds/bg1..4.jpg`, `registration.jpg`, `Textures/Headers/news.png`, `changelog.jpg`, `rankings.jpg`, `search.jpg`, `wiki.jpg` | fallback and overlay-header backgrounds (abstract gradients, dunes, rings, aurora) | procedurally generated for scosu with Python (numpy/scipy/Pillow) | scosu (original) |
| `Tracks/circles.osz`, `Tracks/triangles.osz`, `Tracks/welcome.osz`, `Tracks/christmas2024.osz` | intro / menu music, each an .osz with one .osu (artist and creator "scosu", no background) and `audio.ogg`: circles = upbeat electro, 184 BPM, 139.8 s; triangles = driving synthwave, 160 BPM, logo hit at 3011.7 ms, 126.1 s; welcome = calm piano ambient, 120 BPM, 48 s seamless loop; christmas2024 = festive electro with bells, 172 BPM, drop at 5923.5 ms, 109.2 s (seasonal, disabled in scosu). The intros find them by the hash of the .osu (`BeatmapHash` in `IntroCircles`/`IntroTriangles`/`IntroWelcome`/`IntroChristmas`; `ScosuIntroTracksTest` keeps both in sync) | composed and synthesised for scosu with Python (numpy/scipy/numba: saw/FM/additive-piano voices, filtered-noise drums, convolution reverb, limiter; `ffmpeg` libvorbis); generator scripts kept outside the repo | scosu (original, no third-party audio) |
| `Samples/Intro/welcome.mp3`, `Samples/Intro/Welcome/welcome.mp3`, `Samples/Intro/seeya.mp3`, `Samples/Intro/Welcome/seeya.mp3`, `Samples/Intro/Welcome/welcome_piano.mp3` | non-verbal intro stingers in place of the spoken voices (bright chord swell with bells; piano arpeggio; falling bell chord; falling piano figure) and the 2.14 s rolled-piano chord that leads into the welcome track | synthesised for scosu with Python (numpy/scipy/numba, `ffmpeg` libmp3lame) | scosu (original, no third-party audio) |
| `Tracks/rankedplay_bgm.ogg` | ranked play background music: moody downtempo electro in C minor, 116 BPM, 32 bars = 66.2 s, seamless loop | composed and synthesised for scosu with Python (numpy/scipy/numba, `ffmpeg` libvorbis); generator kept outside the repo | scosu (original, no third-party audio) |
