#!/bin/bash
set -e

echo "Deploying PROVIA..."
git pull origin main
docker compose up --build -d
echo "Done. Check logs: docker compose logs -f app"
