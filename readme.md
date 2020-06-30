# AuthService

[Identity Server](https://identityserver.io)

## Run

<details>
<summary>Command Line</summary>

#### Prerequisites

* [.NET Core SDK](https://aka.ms/dotnet-download)

#### Steps

1. Open directory **source\AuthService** in command line and execute **dotnet run**.
2. Open <https://localhost:5000>.

</details>

<details>
<summary>Visual Studio Code</summary>

#### Prerequisites

* [.NET Core SDK](https://aka.ms/dotnet-download)
* [Visual Studio Code](https://code.visualstudio.com)
* [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)

#### Steps

1. Open **source** directory in Visual Studio Code.
2. Press **F5**.

</details>

<details>
<summary>Visual Studio</summary>

#### Prerequisites

* [Visual Studio](https://visualstudio.microsoft.com)

#### Steps

1. Open **source\AuthService.sln** in Visual Studio.
2. Set **AuthService** as startup project.
3. Press **F5**.

</details>

## Certificate

dotnet dev-certs https -ep "Certificate.pfx" -p 123456 --trust

## Migrations

Add-Migration Application -c ApplicationDbContext -o Migrations/Application

Add-Migration Configuration -c ConfigurationDbContext -o Migrations/Configuration

Add-Migration PersistedGrant -c PersistedGrantDbContext -o Migrations/PersistedGrant
