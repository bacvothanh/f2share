# F2Share

F2Share is a decentralized, cross-platform desktop app for high-performance peer-to-peer folder synchronization.

## Highlights

- Pure P2P sync architecture (LAN discovery + internet peer connectivity)
- .NET 8 backend and Avalonia desktop UI
- Event-driven file change pipeline with debounce and batching
- Chunked + delta transfer planning and resumable transfers
- Conflict detection with version vectors and deterministic conflict renaming
- Local metadata persistence with SQLite
- End-to-end encrypted transport-ready abstractions

## Projects

- `src/F2Share.Domain`: domain entities, value objects, conflict policies
- `src/F2Share.Application`: orchestration, sync engine, transfer planning, contracts
- `src/F2Share.Infrastructure`: persistence, watcher integration, discovery adapters
- `src/F2Share.Transport`: binary protocol contracts and QUIC transport adapter
- `src/F2Share.Desktop`: Avalonia UI host and dashboards
- `tests/F2Share.UnitTests`: pure unit tests for deterministic logic
- `tests/F2Share.IntegrationTests`: end-to-end flow tests across services

## Local setup

1. Install .NET 8 SDK.
2. From repository root:
   - `dotnet restore`
   - `dotnet build -c Release`
   - `dotnet test -c Release`
3. Run UI:
   - `dotnet run --project src/F2Share.Desktop/F2Share.Desktop.csproj`

## Security model (implementation-ready)

- Each node owns a long-lived identity keypair.
- Initial trust is established by room key or out-of-band fingerprint validation.
- Transport is encrypted and mutually authenticated.
- File data and metadata messages are signed and encrypted.

See `docs/architecture.md` for full architecture, data model, sequence diagrams, and deployment guidance.
