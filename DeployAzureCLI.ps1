$SUBSCRIPTION = "My Azure Subscription"

# log in and choose the subscription you want to work with
az login
az account set -s $SUBSCRIPTION

$RESOURCE_GROUP = "azure-funcs-deep-dive"
$LOCATION = "northeurope" # change this to a location near you, (use az account list-locations -o table)

# create a resource group
az group create --name $RESOURCE_GROUP `
    --location $LOCATION

$RANDOM_IDENTIFIER = "8125" # replace this with your own random number

$STORAGE_ACC_NAME = "azdd$RANDOM_IDENTIFIER" 
# create a storage account
az storage account create --name $STORAGE_ACC_NAME `
    --resource-group $RESOURCE_GROUP `
    --sku "Standard_LRS"

# note: app insights creation is now automatically part of az functionapp create

# create a new function app using the consumption plan
$FUNCTION_APP = "azdd$RANDOM_IDENTIFIER" # also needs to be globally unique

az functionapp create -n $FUNCTION_APP `
    --resource-group $RESOURCE_GROUP `
    --storage-account $STORAGE_ACC_NAME `
    --consumption-plan-location $LOCATION `
    --functions-version "4" `
    --runtime "dotnet-isolated" `
    --runtime-version "8"

# our function app uses a cosmos db database
# for samples see https://learn.microsoft.com/en-us/azure/cosmos-db/scripts/cli/nosql/serverless
$COSMOSDB_ACCOUNT = "azdd$RANDOM_IDENTIFIER"
$COSMOSDB_DATABASE = "azurefuncs"
$COSMOSDB_CONTAINER = "orders"
$COSMOSDB_PARTITIONKEY = "/customerEmail"
az cosmosdb create --name $COSMOSDB_ACCOUNT `
    --resource-group $RESOURCE_GROUP `
    --default-consistency-level Eventual `
    --locations regionName="$LOCATION" `
    failoverPriority=0 isZoneRedundant=False `
    --capabilities EnableServerless

# create a CosmosDB database
az cosmosdb sql database create --account-name $COSMOSDB_ACCOUNT `
    --resource-group $RESOURCE_GROUP `
    --name $COSMOSDB_DATABASE

# create the container
az cosmosdb sql container create --account-name $COSMOSDB_ACCOUNT `
    --resource-group $RESOURCE_GROUP `
    --database-name $COSMOSDB_DATABASE `
    --name $COSMOSDB_CONTAINER `
    --partition-key-path $COSMOSDB_PARTITIONKEY


# Get the Azure Cosmos DB connection string.
#az cosmosdb show --name $COSMOSDB_ACCOUNT --resource-group $RESOURCE_GROUP
$COSMOSDB_CONNECTION_STRING = az cosmosdb keys list `
    --name $COSMOSDB_ACCOUNT `
    --resource-group $RESOURCE_GROUP `
    --type "connection-strings" `
    --query "connectionStrings[0].connectionString" `
    -o tsv


# Configure function app settings to use the Azure Cosmos DB connection string.
az functionapp config appsettings set `
    --name $FUNCTION_APP `
    --resource-group $RESOURCE_GROUP `
    --setting "CosmosDbConnection=$COSMOSDB_CONNECTION_STRING"

# To clean up...
az group delete --name $RESOURCE_GROUP