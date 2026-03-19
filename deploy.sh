#!/bin/bash
# ============================================================
# PROVIA - Server Deployment Script
# Run this on your server to update PROVIA to the latest code
#
# First-time setup:
#   chmod +x deploy.sh
#
# Updating the app:
#   ./deploy.sh
# ============================================================

set -e

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
APP_NAME="provia"

echo ""
echo "============================================"
echo "  PROVIA Deployment - $(date)"
echo "============================================"

# Pull latest code from GitHub
echo ""
echo "[1/4] Pulling latest code from GitHub..."
git pull origin main

# Build the new Docker image
echo ""
echo "[2/4] Building updated Docker image..."
docker compose build --no-cache app

# Restart the app container with zero database downtime
# db and nginx are untouched — only the app is restarted
echo ""
echo "[3/4] Restarting app container..."
docker compose up -d --no-deps app

# Show status
echo ""
echo "[4/4] Deployment complete. Container status:"
docker compose ps

echo ""
echo "============================================"
echo "  PROVIA is running."
echo "  Check logs: docker compose logs -f app"
echo "============================================"
echo ""
