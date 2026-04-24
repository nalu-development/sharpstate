# Contributing to Nalu.SharpState

This document is for **developers** working on the library, running tests, packing NuGet packages, or publishing documentation.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) matching [global.json](global.json) (pinned version + roll-forward policy).

## Build, test, and pack

From the repository root:

```bash
dotnet tool restore
dotnet cake --target=Build
dotnet cake --target=Test
dotnet cake --target=Pack
```

You can also open [Nalu.SharpState.slnx](Nalu.SharpState.slnx) in Visual Studio or Rider and build the solution there.

## Documentation (DocFX)

Conceptual Markdown lives under [docs/](docs/). The published site is built with **DocFX** on each push to `main`, mirroring the [nalu](https://github.com/nalu-development/nalu) repository.

### GitHub Pages

In the GitHub repository: **Settings → Pages → Build and deployment**, set **Source** to **GitHub Actions** so the `deploy-gh-pages` workflow can publish.

The site is configured for the `/sharpstate` base path (`_appBasePath` in [docfx.json](docfx.json)).

### Build documentation locally

```bash
dotnet tool restore
dotnet docfx metadata docfx.json
dotnet docfx build docfx.json
```

Open `_site/index.html` in a browser to preview.

## Pull requests

Keep changes focused, match existing style, and ensure `dotnet cake` Build and Test succeed before submitting.
