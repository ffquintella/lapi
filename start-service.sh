#!/bin/bash

set -e

/opt/puppetlabs/puppet/bin/puppet apply  --modulepath=/etc/puppet/modules /etc/puppet/manifests/start.pp



while [ ! -f /var/log/lapi/internal-nlog.txt ]
do
  sleep 2
done
ls -l /var/log/lapi/internal-nlog.txt

tail -n 0 -f /var/log/lapi/internal-nlog.txt &
wait