source ~/.bash_profile

cd /minitwit

docker compose -f docker-compose.yml pull
docker compose -f docker-compose.yml up -d


sudo ufw allow 22
sudo ufw allow 3000
sudo ufw allow 9090
sudo ufw enable