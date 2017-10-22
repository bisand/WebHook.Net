yum -y -q install git
git clone https://github.com/bisand/WebHook.Net.git docker-build

cd docker-build
mkdir -p sshkeys
rm -f sshkeys/*

ssh-keygen -b 4096 -t rsa -f sshkeys/id_rsa -q -N ''
cat sshkeys/id_rsa.pub >> ~/.ssh/authorized_keys

docker build -t bisand/public:webhook.net-server .

cd ..
rm -R -f docker-build
