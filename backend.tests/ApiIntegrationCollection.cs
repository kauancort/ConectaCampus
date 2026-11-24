using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AprendizagemColaborativa.Api.Tests;

// Uma API por banco de teste evita startups concorrentes sobre o mesmo schema vazio.
// Os testes de concorrência continuam enviando requisições simultâneas à API.
[CollectionDefinition(ApiIntegrationCollection.Name)]
public sealed class ApiIntegrationCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
    public const string Name = "API com PostgreSQL";
}
