var builder = DistributedApplication.CreateBuilder(args);

// --- SQL Server ---
var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sqlServer = builder.AddSqlServer("sql", password: sqlPassword, port: 1433)
    .WithImageTag("2019-latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataBindMount("../.data/MSSQL2019-DATA");

var storeDb = sqlServer.AddDatabase("DefaultConnectionMssql", databaseName: "eCommNetDb");
var identityDb = sqlServer.AddDatabase("IdentityConnectionMssql", databaseName: "eCommNet_IdentityDb");

// --- Redis ---
var redis = builder.AddRedis("redis", port: 6379);

// --- Seq (generic container) ---
var seq = builder.AddContainer("seq", "datalust/seq", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithBindMount("../.data/seq-data", "/data")
    .WithHttpEndpoint(port: 5341, targetPort: 5341, name: "SeqIngest")
    .WithHttpEndpoint(port: 8081, targetPort: 80, name: "SeqUi")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .ExcludeFromManifest();

// --- Api ---
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(storeDb)
    .WithReference(identityDb)
    .WithReference(redis)
    .WaitFor(sqlServer)
    .WaitFor(redis)
    .WaitFor(seq)
    .WithEnvironment("Seq__ServerUrl", () => seq.GetEndpoint("SeqIngest").Url)
    .WithEnvironment("AllowedOrigins", "http://localhost:4200");

// --- SeedData (opt-in) ---
bool includeSeedData = bool.TryParse(
    builder.Configuration["ASPIRE_INCLUDE_SEEDDATA"], out bool sd) && sd;

if (includeSeedData)
{
    builder.AddProject<Projects.SeedData>("seeddata")
        .WithReference(storeDb)
        .WithReference(identityDb)
        .WaitFor(sqlServer)
        .ExcludeFromManifest();
}

// --- Angular (opt-in) ---
bool includeAngular = bool.TryParse(
    builder.Configuration["ASPIRE_INCLUDE_ANGULAR"], out bool ang) && ang;

if (includeAngular)
{
    builder.AddDockerfile("angular", "../../client", "Dockerfile.aspire")
        .WithBindMount("../../client/src", "/app/src")
        .WithHttpEndpoint(port: 4200, targetPort: 4200)
        .WaitFor(api)
        .ExcludeFromManifest();
}

builder.Build().Run();
