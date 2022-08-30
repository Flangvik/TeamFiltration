#!/bin/bash
# delete_fireprox_instances.sh

# https://stackoverflow.com/questions/1527049/how-can-i-join-elements-of-an-array-in-bash
function join_by { local IFS="$1"; shift; echo "$*"; }

# Possible AWS regions
regionsArray=( us-east-1 us-west-1 us-west-2 ca-central-1 eu-central-1 eu-west-1 eu-west-2 eu-west-3 eu-north-1 )

#Endpoint to be proxied
msolEndpoint="https://login.microsoftonline.com/"
msolEndpointUs="https://login.microsoftonline.us/"
teamsEnumEndpoint="https://teams.microsoft.com/api/mt/"
aadSSoEndpoint="https://autologon.microsoftazuread-sso.com/"

AWSACCESSKEY="YOUR_AWS_ACCESS_KEY"
AWSSECRETKEY="YOUR_AWS_SECRET_KEY"


declare -a msolFireProxEndpoints
declare -a msolFireProxEndpointsUs
declare -a teamsEnumFireProxEndpoints
declare -a aadSSoFireProxEndpoints

#generate them all
for i in "${regionsArray[@]}"
do
        for t in $(python3 fire.py --access_key $AWSACCESSKEY --secret_access_key $AWSSECRETKEY --region $i --command list | grep fireprox | cut -d'(' -f2 | cut -d')' -f1 )
        do
                python3 fire.py --access_key $AWSACCESSKEY --secret_access_key $AWSSECRETKEY --region $i --command delete --api_id $t
        done
done

echo "Checking available instances...."
for i in "${regionsArray[@]}"
do
        python3 fire.py --access_key $AWSACCESSKEY --secret_access_key $AWSSECRETKEY --region $i --command list
done

