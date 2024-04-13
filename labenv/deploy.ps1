# to test deploying this ARM template
$RESOURCE_GROUP = "ps-lab-test"
az group create -l "westeurope" -n $RESOURCE_GROUP
az deployment group create --resource-group $RESOURCE_GROUP --template-file "funcapp-arm.json"

# to produce a zip
# note for the hands-on lab cloud environment, set the timer trigger to 1 minute
# and the create ticket activity to 30 sec
dotnet publish -c Release
$publishFolder = "bin/Release/net8.0/publish"
$publishZip = "labenv/publish.zip"
if (Test-path $publishZip) { Remove-item $publishZip }
Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($publishFolder, $publishZip)