#!/bin/bash
set -e

echo "=== PROVIA post-merge setup ==="

# Install React dependencies if package.json changed
cd JobTracker/ClientApp
npm install --legacy-peer-deps --prefer-offline 2>/dev/null || true
cd ../..

echo "=== Post-merge setup complete ==="
