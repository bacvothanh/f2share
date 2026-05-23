# Deployment Guide

## 1. Build Matrix

Target Runtime Identifiers:

- win-x64
- linux-x64
- osx-x64
- osx-arm64

## 2. Publish Commands

From repository root:

- dotnet publish src/F2Share.Desktop/F2Share.Desktop.csproj -c Release -r win-x64 --self-contained true
- dotnet publish src/F2Share.Desktop/F2Share.Desktop.csproj -c Release -r linux-x64 --self-contained true
- dotnet publish src/F2Share.Desktop/F2Share.Desktop.csproj -c Release -r osx-arm64 --self-contained true

## 3. Recommended Packaging

- Windows: MSIX or signed installer (WiX/Advanced Installer).
- Linux: AppImage, deb/rpm.
- macOS: signed notarized .app bundle.

## 4. Config and Data Paths

- App config directory:
  - Windows: %APPDATA%/F2Share
  - Linux: ~/.config/F2Share
  - macOS: ~/Library/Application Support/F2Share
- Metadata DB and queue state must be stored under user profile path.

## 5. Auto-update Strategy

- Host signed update manifests in static object storage.
- App periodically checks channel endpoint.
- Validate signature before applying update.
- Support staged rollouts by channel:
  - stable
  - preview
  - canary

## 6. Enterprise Rollout

- Provide silent install switches.
- Pre-seed trusted peer fingerprints and room keys.
- Ship default ignore rules and bandwidth policies.

## 7. Runtime Hardening

- Restrict incoming message size.
- Enforce path normalization before file operations.
- Run with least privilege.
- Enable audit logging and rotation.
