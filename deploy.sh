docker stop webhook
docker rm webhook
docker run -d -it -p 7777:7777 --name=webhook bisand/public:webhook-server
