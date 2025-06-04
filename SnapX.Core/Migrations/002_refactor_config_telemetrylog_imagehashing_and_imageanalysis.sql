-- Migration: 002_refactor_config_telemetrylog_imagehashing_and_imageanalysis.sql
-- Created: 2025-06-04
-- Description: This migration refactors and adds configuration-related tables,
-- introduces image hashing and analysis support, and adds a telemetry event log.
--
-- Specifically:
-- - Removes legacy tables like SavedConfiguration and RootConfigurationHistories in favor of better-structured ConfigHistory.
-- - Adds support for upload/hotkey configuration via UploadersConfig/HotkeysConfig.
-- - Creates ImageHashes and ImageAnalysis tables to support de-duplication and semantic indexing of captured images.
-- - Adds TelemetryLog for structured storage of diagnostic and behavioral events, for transparency with users.

-- Useless table
-- SnapX hasn't put anything into this table, ever.
DROP TABLE IF EXISTS SavedConfiguration;

-- Poorly designed, replaced by ConfigHistory
-- SnapX hasn't put anything into this table, ever.
DROP TABLE IF EXISTS RootConfigurationHistories;

-- I really hope any testers haven't inserted precious data into this.
-- SnapX hasn't put anything into this table, ever.
DROP TABLE IF EXISTS Tag;


CREATE TABLE IF NOT EXISTS ConfigHistory (
  Id INTEGER NOT NULL CONSTRAINT PK_ConfigHistory PRIMARY KEY AUTOINCREMENT,
  Configuration TEXT NOT NULL,
  SettingKey TEXT NOT NULL,
  OldValue TEXT NOT NULL,
  NewValue TEXT NOT NULL,
  DataType TEXT NOT NULL,
  ConfigSection TEXT NOT NULL,
  ModifiedAt TEXT NOT NULL,
  ChangedBy TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Tags (
 Id INTEGER NOT NULL CONSTRAINT PK_Tags PRIMARY KEY AUTOINCREMENT,
 Text TEXT NOT NULL,
 WindowTitle TEXT NULL,
 ProcessName TEXT NULL,
 HistoryItemId INTEGER NULL,
 CONSTRAINT "FK_Tags_HistoryItems_HistoryItemId" FOREIGN KEY ("HistoryItemId") REFERENCES "HistoryItems" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS UploadersConfig (
 Id INTEGER NOT NULL CONSTRAINT PK_UploadersConfig PRIMARY KEY AUTOINCREMENT,
 SettingKey TEXT NOT NULL,
 SettingValue TEXT NOT NULL,
 ConfigSection TEXT NOT NULL,
 DataType TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS HotkeysConfig (
  Id INTEGER NOT NULL CONSTRAINT PK_HotkeysConfig PRIMARY KEY AUTOINCREMENT,
  SettingKey TEXT NOT NULL,
  SettingValue TEXT NOT NULL,
  ConfigSection TEXT NOT NULL,
  DataType TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ImageHashes (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 ImageHash TEXT NOT NULL UNIQUE,
 CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
 FileSize INTEGER, -- KiB
 FileFormat TEXT -- mime type
);

CREATE TABLE IF NOT EXISTS ImageAnalysis (
 Id INTEGER PRIMARY KEY AUTOINCREMENT,
 ImageHashId INTEGER NOT NULL,
 OcrText TEXT,
 Summary TEXT,
 ProcessedAt TEXT NOT NULL DEFAULT (datetime('now')),
 ProcessedBy TEXT,
 Keywords TEXT, -- Comma seperated list of tags to be applied once ImageHash matches. Will be added to Tags table to the respective HistoryItem
 CONSTRAINT "FK_ImageAnalysis_ImageHashes_ImageHashId" FOREIGN KEY (ImageHashId) REFERENCES ImageHashes(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS TelemetryLog  (
  Id             INTEGER PRIMARY KEY AUTOINCREMENT,
  EventName      TEXT NOT NULL,
  EventTimestamp TEXT NOT NULL DEFAULT (datetime('now')),
  Provider       TEXT NOT NULL,
  Envelope       JSON NOT NULL
);
