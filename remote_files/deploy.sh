#!/usr/bin/env bash
source ~/.bash_profile

set -euo pipefail

: "${DOCKER_USERNAME:?DOCKER_USERNAME is not set in ~/.bash_profile}"
: "${DOCKER_PASSWORD:?DOCKER_PASSWORD is not set in ~/.bash_profile}"
: "${CHIRP_DB_CONNECTION:?CHIRP_DB_CONNECTION is not set in ~/.bash_profile}"

cd /minitwit

echo "${DOCKER_PASSWORD}" | docker login --username "${DOCKER_USERNAME}" --password-stdin
docker compose -f docker-compose.yml pull minitwit
docker compose -f docker-compose.yml up -d


sudo ufw allow 22
sudo ufw allow 3000
sudo ufw allow 9090
sudo ufw enable
