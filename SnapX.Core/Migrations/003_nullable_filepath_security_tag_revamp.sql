-- Migration: 003_nullable_filepath_security_tag_revamp.sql
-- Created: 2025-06-05
-- Description: Overhauls the HistoryItems table, cleans up old configurations, and introduces new error logging and response tracking.
--
-- The HistoryItems table is rebuilt to enforce a 255-character limit on FileName and explicitly mark FilePath and Type as nullable, reflecting their optional nature in the application.
-- Existing history data is preserved and migrated.
-- Obsolete configuration tables (ApplicationConfig, UploadersConfig, HotkeysConfig) are permanently removed.
--
-- Two new tables are introduced: UploadErrors and DestinationLatestResponse.
-- UploadErrors will log comprehensive details about failed uploads, including timestamps, destinations, file info, error messages, stack traces, payloads, and HTTP status codes, helping with debugging and user support.
-- DestinationLatestResponse will store the most recent successful JSON response for each upload destination, intended to power future auto-completion features.
-- This makes the database more robust, streamlined, and ready for enhanced error handling.
ALTER TABLE HistoryItems RENAME TO _old_HistoryItems;

CREATE TABLE IF NOT EXISTS HistoryItems (
  Id INTEGER NOT NULL CONSTRAINT PK_HistoryItems PRIMARY KEY AUTOINCREMENT,
  FileName TEXT NOT NULL CHECK (length(FileName) <= 255), -- Now enforcing common file name limits
  FilePath TEXT NULL, -- Actually is nullable and treated as such in SnapX.Core
  DateTime TEXT DEFAULT CURRENT_TIMESTAMP NOT NULL,
  Type TEXT NULL, -- now nullable
  Host TEXT NULL,
  URL TEXT NULL,
  ThumbnailURL TEXT NULL,
  DeletionURL TEXT NULL,
  ShortenedURL TEXT NULL,
  Hidden BOOLEAN NOT NULL DEFAULT 0 CHECK (Hidden IN (0, 1)) -- BOOLEAN is for readability, to SQLite it's just a type with NUMERIC affinity.
);


INSERT INTO HistoryItems (
  Id, FileName, FilePath, DateTime, Type, Host, URL, ThumbnailURL, DeletionURL, ShortenedURL, Hidden
)

SELECT
  Id, FileName, FilePath, DateTime, Type, Host, URL, ThumbnailURL, DeletionURL, ShortenedURL, Hidden
FROM _old_HistoryItems;

DROP TABLE IF EXISTS _old_HistoryItems;

-- Users would've hated this anyways.
DROP TABLE IF EXISTS ApplicationConfig;
DROP TABLE IF EXISTS UploadersConfig;
DROP TABLE IF EXISTS HotkeysConfig;


CREATE TABLE IF NOT EXISTS UploadErrors (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Timestamp      TEXT NOT NULL DEFAULT (datetime('now')),
  Destination    TEXT NOT NULL,                -- e.g., "Imgur", "Dropbox"
  FileName       TEXT NOT NULL,
  FilePath       TEXT,
  ErrorMessage   TEXT NOT NULL,
  StackTrace     TEXT,                          -- if applicable, helpful for debugging
  RequestPayload TEXT,                      -- serialized request
  ResponsePayload TEXT,                     -- raw response from server
  HttpStatusCode  INTEGER NOT NULL CHECK (
    HttpStatusCode IS NULL OR (HttpStatusCode >= 100 AND HttpStatusCode <= 599)
  ),
  Resolved BOOLEAN NOT NULL DEFAULT 0 CHECK (Resolved IN (0, 1)) -- has user taken action / reviewed it
);

-- Success responses only. This is for JSON auto complete down the line.
CREATE TABLE IF NOT EXISTS DestinationLatestResponse (
 Destination TEXT PRIMARY KEY UNIQUE,               -- Unique destination name (e.g., "Imgur", "Dropbox")
 Response JSON NOT NULL,                            --
 Timestamp TEXT NOT NULL DEFAULT (datetime('now'))  -- When this response was stored
);
