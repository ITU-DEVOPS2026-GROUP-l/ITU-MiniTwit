## Setting up and running Chirp! locally

### Step 1: Clone the repository:
Clone the [Chirp! Repository](https://github.com/ITU-BDSA2025-GROUP21/Chirp) to your local machine using Git.


### Step 2: Ensure the correct .NET version is installed:
Verify that .NET SDK 8.0 is installed on your system. The project targets .NET 8.0 and is not compatible with earlier versions.

### Step 3: Restore project dependencies:
From the root of the solution, restore all required dependencies by running this .NET CLI command:
```
dotnet restore
```

### Step 4: Configure the authentication secrets:
Before running the application, configure the GitHub authentication secrets using the .NET user-secrets feature. These secrets are required for authentication to function correctly.
```
dotnet user-secrets set "Authentication:GitHub:ClientSecret" "bcae20e854008588fdc845fe904f43d7a204c424"
dotnet user-secrets set "Authentication:GitHub:ClientId" "Ov23li7jj1fMsnWTWXzO"
```

### Step 5: Choose a database provider
Development defaults to SQLite through `src/Chirp.Web/appsettings.Development.json`.

Production and Docker are configured for PostgreSQL through `src/Chirp.Web/appsettings.json` and `src/Chirp.Web/appsettings.Production.json`.

If you want to run PostgreSQL locally, start a database first:
```
docker run --name chirp-postgres -e POSTGRES_DB=chirp -e POSTGRES_USER=chirp -e POSTGRES_PASSWORD=chirp -p 5432:5432 -d postgres:16
```

### Step 6: Build and run the application
Navigate to the `src/Chirp.Web` project directory and start the application by running:
```
dotnet run
```

### Step 7: Access the application
Once the application has started successfully, the console will display a localhost URL. Open this URL in a web browser to access the Chirp! application.

### Running Test Suite locally
To validate the system locally, the automated test suite can be executed by running 
```
dotnet test
```
from the root of the Chirp! solution. This command will build all the required projects. 
To run the Playwrights tests, you must first install playwright using the following commands in your terminal:
```
npm install -D @playwright/test
npx playwright install
```
Then when you use 
```
dotnet test
```
The playwright tests will also run.
