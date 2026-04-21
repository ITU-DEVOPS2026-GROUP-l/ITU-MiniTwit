# firewall setup with ufw

## Overview
Both of our servers are protected with UFW on Ubuntu

## Servers 

### Main Server: minitwit-ci-server (161.35.211.34)

Port   | Action | Description
-------|--------|-------------
5000   | Allow  | MiniTwit app
22/tcp | Limit 	| ssh (rate-limited)
3000   | Allow  | Grafana
9090   | Allow  | Prometheus
80,443/tcp | Allow | Http/https (Nginx Full)


### Backup server: ubuntu-droplet-2(164.92.246.143) 
Port   | Action | Description
-------|--------|-------------
22/tcp | Limit 	| ssh (rate-limited)
80/tcp | Allow  | http
443/tcp| Allow  | https
5000   | Allow  | MiniTwit app
3000   | Allow  | Grafana
9090   | Allow  | Prometheus

## Setup commands used
```
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw limit ssh
sudo ufw allow http
sudo ufw allow https
sudo ufw allow 'Nginx Full'
sudo ufw allow 5000
sudo ufw allow 3000
sudo ufw allow 9090
sudo ufw enable
```

## Notes
- SSH is rate-limited
- IPv6 rules are automatically applied alongside IPv4
- Logging is enabled on both servers
