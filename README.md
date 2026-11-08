# Rasta – Persian RTL Comments

**Rasta** (راستا — "direction, alignment") is a Visual Studio extension that shows Persian/Arabic code comments, mixed with English words, identifiers, numbers and punctuation, correctly right-to-left in a hover popup.

The editor lays comment text out left-to-right, so Persian sentences with embedded English look scrambled. Hover over such a comment and Rasta shows a QuickInfo popup that renders it properly.

## Before / after

Editor (left-to-right layout):

```
// مقدار Timeout باید بیشتر از 30 ثانیه باشه (طبق RFC 7230)
```

Rasta popup (right-to-left, English runs isolated):

```
        مقدار Timeout باید بیشتر از 30 ثانیه باشه (طبق RFC 7230)
```

## Install

1. Build the solution in Release (see below) or download `Rasta.vsix`.
2. Close Visual Studio, double-click `Rasta.vsix` and follow the VSIX Installer.
3. Restart Visual Studio. Supported: Visual Studio 2022 (17.x) and 2026 (18.x), amd64 and arm64.

Build: `msbuild Rasta.sln /restore /p:Configuration=Release` → `src\Rasta\bin\Release\Rasta.vsix`.

## Debug

Open `Rasta.sln`, set `Rasta` as startup project and press **F5**. It starts the Experimental Instance (`devenv.exe /rootsuffix Exp`) with the extension deployed.

## Options (Tools ▸ Options ▸ Rasta ▸ General)

| Option | Default |
|---|---|
| Enabled | on |
| Font family | `Vazirmatn, Segoe UI, Tahoma` |
| Font size | 14 |
| Max popup width | 600 |
| Minimum RTL characters | 1 |
| Force RTL when Persian is present | on |
| Merge consecutive comment lines | on |
| Show Rasta header | on |

Changes apply to the next hover; no restart is needed.

## Features

- Comment detection through the editor's classifier (no text searching): `//`, `///`, `/* */`, XML doc, `<!-- -->`, Razor, SQL `--`, Python `#`, VB `'`.
- Adjacent comment lines (or a whole block) are merged into one paragraph.
- XML doc tags are cleaned: `<param name="x">` becomes `x — …`, `<see cref="Foo"/>` becomes `Foo`, `<c>` becomes inline code.
- Theme-aware (Light, Dark, Blue, High Contrast); selectable text; Copy button and context menu; ZWNJ preserved.
- Coexists with other QuickInfo providers (Roslyn etc.).

## Known limitations

- Only the hovered buffer is classified; in projection buffers (Razor, some XAML) the comment must be classified in that buffer.
- Comments trailing code are never merged with following lines.
- Isolation of English runs uses LRE/RLE…PDF embeddings because WPF's bidi engine predates Unicode isolates.
- The popup appears on hover only; the editor text itself is not changed.
- The extension was verified by building, unit tests and an offscreen WPF render; it was not exercised interactively inside Visual Studio.

## Architecture

- `RastaPackage` – async package, options page, publishes an immutable `RastaSettings` snapshot.
- `QuickInfo/` – MEF `IAsyncQuickInfoSourceProvider` + source; per-buffer cache (`RastaBufferState`).
- `Comments/CommentExtractor` – classifier-based comment span discovery and line merging.
- `Text/` – pure logic: `RtlTextDetector`, `CommentTextCleaner`, `BidiRunSplitter`.
- `UI/RastaCommentView` – WPF popup content.
- `Logging/RastaLog` – Activity Log and the "Rasta" Output pane.
- `tests/Rasta.Tests` – MSTest tests for the pure logic.

## License

MIT © Ali Tajmir Riahi
