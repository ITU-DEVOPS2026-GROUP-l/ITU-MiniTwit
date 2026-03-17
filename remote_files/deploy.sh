source ~/.bash_profile

cd /minitwit

if [ -z "$CHIRP_DB_CONNECTION" ]; then
  echo "CHIRP_DB_CONNECTION is not set. Aborting deployment."
  exit 1
fi

docker compose -f docker-compose.yml pull
docker compose -f docker-compose.yml up -d --remove-orphans


sudo ufw allow 22
sudo ufw allow 3000
sudo ufw allow 9090
sudo ufw enable
