# ABC Retail - Azure Storage Solution

ASP.NET Core MVC (.NET 8) web app demonstrating Azure Table, Blob, Queue, and File storage.

## Setup

1. Open `ABCRetail.csproj` in Visual Studio (or run `dotnet restore` then `dotnet run`).
2. In `appsettings.json`, replace `AzureStorage:ConnectionString` with your Storage Account's connection string
   (Azure Portal → your Storage Account → Access keys → Connection string).
3. Run the app locally (F5 in Visual Studio, or `dotnet run`). Tables, containers, the queue, and the file share
   are created automatically on first use if they don't already exist.
4. Test all four features locally:
   - Customers → adds rows to the `CustomerProfiles` table
   - Products → adds rows to the `Products` table and uploads images to the `product-images` blob container
   - Order Queue → sends/peeks messages on the `order-processing-queue`
   - Log Files → uploads files to the `logfiles` Azure file share
5. Once working locally, deploy to an Azure App Service (see the guide provided alongside this code) and add the
   same connection string as an Application Setting on the App Service.
6. Push this code to a GitHub repository and include the repo link and the deployed URL in your submission document.
