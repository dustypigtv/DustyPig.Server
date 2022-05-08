#!/bin/bash


mkdir -p _work_

wget -O _work_/DustyPig.Server.zip https://github.com/dustypigtv/DustyPig.Server/releases/latest/download/DustyPig.Server.zip

unzip _work_/DustyPig.Server.zip -d _work_/server

sudo systemctl stop service-dustypig-tv
sudo mkdir -p /var/www/service.dustypig.tv
sudo rclone sync _work_/server /var/www/service.dustypig.tv -v --checksum
sudo chmod +x /var/www/service.dustypig.tv/DustyPig.Server
sudo systemctl start service-dustypig-tv

rm -rf _work_
