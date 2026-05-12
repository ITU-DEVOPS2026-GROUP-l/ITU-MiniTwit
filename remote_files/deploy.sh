#!/usr/bin/env bash
set -e
source ~/.bash_profile

cd /minitwit

if [ -z "$CHIRP_DB_CONNECTION" ]; then
  echo "CHIRP_DB_CONNECTION is not set. Aborting deployment."
  exit 1
fi

if [ -z "$DOCKER_USERNAME" ]; then
  echo "DOCKER_USERNAME is not set. Aborting deployment."
  exit 1
fi

# Pull latest images
docker compose pull minitwit-1 minitwit-2

# Rolling update: restart one instance at a time so traffic keeps flowing
echo "Updating minitwit-1..."
docker compose up -d --no-deps minitwit-1
sleep 15

echo "Updating minitwit-2..."
docker compose up -d --no-deps minitwit-2
sleep 15

# Bring up everything else (no-op if already running)
docker compose up -d --remove-orphans

# Firewall rules
sudo ufw allow 22
sudo ufw allow 5000
sudo ufw allow 3000
sudo ufw allow 9090
sudo ufw --force enable

echo "Deploy complete."
docker compose ps
