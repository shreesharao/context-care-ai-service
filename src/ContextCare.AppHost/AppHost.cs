using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", value: "postgres");
var password = builder.AddParameter("password", value: "db@123");
var postgres = builder.AddPostgres("postgres")
       .WithImage("pgvector/pgvector:pg17")
       .WithUserName(username)
       .WithPassword(password)
       .WithHostPort(5432)
       .AddDatabase("ContextCareDb");

builder.AddProject<ContextCare_Api>("api")
       .WithReference(postgres)
       .WaitFor(postgres);

await builder.Build().RunAsync();
