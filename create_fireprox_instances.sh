#!/bin/bash
# create_fireprox_instances.sh

# https://stackoverflow.com/questions/1527049/how-can-i-join-elements-of-an-array-in-bash
function join_by { local IFS="$1"; shift; echo "$*"; }

# Possible AWS regions
regionsArray=( us-east-1 us-west-1 us-west-2 ca-central-1 eu-central-1 eu-west-1 eu-west-2 eu-west-3 eu-north-1 )
#regionsArray=( us-east-1 )

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
        newMsolEndpoint=$( python3 fire.py --access_key $AWSACCESSKEY --secret_access_key $AWSSECRETKEY --region $i --command create --url $msolEndpoint | grep -iPo "https?:\/\/(www\.)?[-a-zA-Z0-9]{10}.*\.amazonaws\.com\/fireprox\/" )
        msolFireProxEndpoints=("${msolFireProxEndpoints[@]}" "\"$newMsolEndpoint""common/oauth2/token\"")
        echo "[+] Created $newMsolEndpoint that points to $msolEndpoint"

        newMsolEndpointUs=$(python3 fire.py --access_key $AWSACCESSKEY --secret_access_key $AWSSECRETKEY --region $i --command create --url $msolEndpointUs | grep -iPo "https?:\/\/(www\.)?[-a-zA-Z0-9]{10}.*\.amazonaws\.com\/fireprox\/" )
        msolFireProxEndpointsUs=("${msolFireProxEndpointsUs[@]}" "\"$newMsolEndpointUs"."common/oauth2/token\"")
        echo "[+] Created $newMsolEndpointUs that points to $msolEndpointUs"

        newTeamsEnumEndpoint=$(python3 fire.py --access_key $AWSACCESSKEY --secret_access_key $AWSSECRETKEY --region $i --command create --url $teamsEnumEndpoint | grep -iPo "https?:\/\/(www\.)?[-a-zA-Z0-9]{10}.*\.amazonaws\.com\/fireprox\/" )
        teamsEnumFireProxEndpoints=("${teamsEnumFireProxEndpoints[@]}" "\"$newTeamsEnumEndpoint\"")
        echo "[+] Created $newTeamsEnumEndpoint that points to $teamsEnumEndpoint"

        newAadSSoEndpoint=$(python3 fire.py --access_key $AWSACCESSKEY --secret_access_key $AWSSECRETKEY --region $i --command create --url $aadSSoEndpoint | grep -iPo "https?:\/\/(www\.)?[-a-zA-Z0-9]{10}.*\.amazonaws\.com\/fireprox\/" )
        aadSSoFireProxEndpoints=("${aadSSoFireProxEndpoints[@]}" "\"$newAadSSoEndpoint\"")
        echo "[+] Created $newAadSSoEndpoint that points to $aadSSoEndpoint"

done

        echo "[+] Done, printing jsonArray(s)"
        echo ""

        msolFireProxEndpointsJsonArray=$(join_by , ${msolFireProxEndpoints[@]})
        echo "\"msolFireProxEndpoints\": [$msolFireProxEndpointsJsonArray],"

        msolFireProxEndpointsUsJsonArray=$(join_by , ${msolFireProxEndpointsUs[@]})
        echo "\"msolFireProxEndpointsUs\": [$msolFireProxEndpointsUsJsonArray],"

        teamsEnumFireProxEndpointsJsonArray=$(join_by , ${teamsEnumFireProxEndpoints[@]})
        echo "\"teamsEnumFireProxEndpoints\": [$teamsEnumFireProxEndpointsJsonArray],"

        aadSSoFireProxEndpointsJsonArray=$(join_by , ${aadSSoFireProxEndpoints[@]})
        echo "\"aadSSoFireProxEndpoints\": [$aadSSoFireProxEndpointsJsonArray],"
(venv) root@phserver:/opt/fireprox#

