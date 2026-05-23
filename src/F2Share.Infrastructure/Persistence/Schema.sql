CREATE TABLE IF NOT EXISTS file_fingerprints (
    share_id TEXT NOT NULL,
    relative_path TEXT NOT NULL,
    length INTEGER NOT NULL,
    last_write_utc TEXT NOT NULL,
    strong_hash TEXT NOT NULL,
    block_hashes TEXT NOT NULL,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (share_id, relative_path)
);

CREATE TABLE IF NOT EXISTS sync_queue (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    share_id TEXT NOT NULL,
    peer_device_id TEXT NOT NULL,
    relative_path TEXT NOT NULL,
    operation_kind INTEGER NOT NULL,
    payload BLOB NULL,
    retry_count INTEGER NOT NULL DEFAULT 0,
    next_attempt_utc TEXT NOT NULL,
    created_utc TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_sync_queue_next_attempt ON sync_queue(next_attempt_utc);
