# Azure Functions Deep Dive
## Pluralsight course by Mark Heath

This repository contains the code built for the Azure Functions Deep Dive course on Pluralsight. It uses Azure Functions v4 in the Isolated Process mode.

Recommended development environment is to use:
- [Visual Studio Code](https://code.visualstudio.com/)
- [The C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
- [The Azure Functions VS Code extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions)
- [The Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)


Follow [these instructions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-develop-vs-code) to get set up to develop Azure Functions in Visual Studio code.

### Running Locally

You need Azurite and the Cosmos DB Storage Emulator running locally in order to use all of the functions. If you'd prefer not to use Cosmos DB, you can simply comment out the Cosmos DB output binding from the code.

### Local Settings

To run locally you will need a `local.settings.json` file which is not checked in to source control, but should contain the following values. Note that you also need to put in the correct connection string for the Azure Cosmos DB emulator (which you can get from its UI). The value below is redacted to avoid including the secret key:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosDbConnection": "AccountEndpoint=https://localhost:8081/;AccountKey=<REDACTED>"
  }
}
```

### Useful Links

- [REST Client VS Code Extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)
- [Cosmos DB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-develop-emulator?tabs=windows%2Ccsharp&pivots=api-nosql)
- [Azurite Storage Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage)