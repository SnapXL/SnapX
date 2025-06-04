-- Migration: 001_initial_schema.sql
-- Created: 2025-06-04
-- Description: Initial schema for SnapX config and history tables.

CREATE TABLE IF NOT EXISTS "ApplicationConfig" (
  "Id" INTEGER NOT NULL UNIQUE,
  "SettingKey" TEXT NOT NULL,
  "SettingValue" TEXT NOT NULL,
  "ConfigSection" TEXT NOT NULL,
  "DataType" TEXT NOT NULL,
  CONSTRAINT "PK_RootConfigurations" PRIMARY KEY ("Id" AUTOINCREMENT)
);

CREATE TABLE IF NOT EXISTS "HistoryItems" (
 "Id" INTEGER NOT NULL CONSTRAINT "PK_HistoryItems" PRIMARY KEY AUTOINCREMENT,
 "FileName" TEXT NOT NULL,
 "FilePath" TEXT NOT NULL,
 "DateTime" TEXT NOT NULL,
 "Type" TEXT NOT NULL,
 "Host" TEXT NULL,
 "URL" TEXT NULL,
 "ThumbnailURL" TEXT NULL,
 "DeletionURL" TEXT NULL,
 "ShortenedURL" TEXT NULL,
 "Hidden" INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS "RootConfigurationHistories" (
 "Id" INTEGER NOT NULL CONSTRAINT "PK_RootConfigurationHistories" PRIMARY KEY AUTOINCREMENT,
 "ConfigurationId" INTEGER NOT NULL,
 "SettingKey" TEXT NOT NULL,
 "OldValue" TEXT NOT NULL,
 "NewValue" TEXT NOT NULL,
 "DataType" TEXT NOT NULL,
 "ConfigSection" TEXT NOT NULL,
 "ModifiedAt" TEXT NOT NULL,
 "ChangedBy" TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS "Tag" (
  "Id" INTEGER NOT NULL CONSTRAINT "PK_Tag" PRIMARY KEY AUTOINCREMENT,
  "Text" TEXT NOT NULL,
  "WindowTitle" TEXT NULL,
  "ProcessName" TEXT NULL,
  "HistoryItemId" INTEGER NULL,
  CONSTRAINT "FK_Tag_HistoryItems_HistoryItemId" FOREIGN KEY ("HistoryItemId") REFERENCES "HistoryItems" ("Id")
);

CREATE TABLE IF NOT EXISTS "SavedConfiguration" (
 "Id" INTEGER NOT NULL CONSTRAINT "PK_SavedConfiguration" PRIMARY KEY AUTOINCREMENT,
 "SettingKey" TEXT NOT NULL,
 "SettingValue" TEXT NOT NULL,
 "ConfigSection" TEXT NOT NULL,
 "DataType" TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS MigrationLog (
 "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
 "FileName" TEXT NOT NULL,
 "AppliedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
