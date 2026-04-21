# flowstate-edge-collector# FlowState Edge Collector

Industrial edge application for collecting PLC events and forwarding them to cloud systems in near real-time.

## 🚀 Overview

This service reads trigger-based events from PLC systems, enriches them with contextual data, and forwards them to a cloud database.

It is designed for reliability in industrial environments, handling:

- Intermittent connectivity
- Event ordering
- Retry logic
- Local buffering


## 🔧 Features

- Trigger-based PLC event detection
- Event queueing and retry handling
- Timestamp normalization (UTC)
- Event ordering and prioritization
- Local persistence (optional)
- Cloud SQL integration

## 🌐 Networking

Supports:

- Direct DB connection (port 3306)
- Cloud SQL Proxy (recommended for restricted networks)

## ⚙️ Configuration

Main settings:

- PLC connection parameters
- Database connection
- Retry policies
- Event batching

## ▶️ Running

### Local

```bash
dotnet run
