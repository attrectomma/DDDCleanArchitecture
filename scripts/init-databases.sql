-- ============================================================
-- Initialization script for local development.
-- Creates a separate database per API so they don't interfere.
-- This script runs once when the Postgres container is first created.
-- ============================================================

CREATE DATABASE retroboard_api1;
CREATE DATABASE retroboard_api2;
CREATE DATABASE retroboard_api3;
CREATE DATABASE retroboard_api4;
CREATE DATABASE retroboard_api5;
