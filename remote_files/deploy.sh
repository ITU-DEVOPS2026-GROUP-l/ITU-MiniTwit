#!/usr/bin/env bash
source ~/.bash_profile

cd /minitwit

if [ -z "$CHIRP_DB_CONNECTION" ]; then
  echo "CHIRP_DB_CONNECTION is not set. Aborting deployment."
  exit 1
fi

if [ -z "$IMAGE_TAG" ]; then
  echo "IMAGE_TAG is not set. Aborting deployment."
  exit 1
fi

export IMAGE_TAG

docker compose pull
docker compose up -d --remove-orphans --scale minitwit=2

sudo ufw allow 22
sudo ufw allow 5000
sudo ufw allow 3000
sudo ufw allow 9090
sudo ufw allow 'Nginx Full'
sudo ufw enable
