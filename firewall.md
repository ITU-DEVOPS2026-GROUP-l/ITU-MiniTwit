# firewall setup with ufw

## Overview
Both of our servers are protected with UFW on Ubuntu

##Servers 

### minitwit-ci-server (161.35.211.34)

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

