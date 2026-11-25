using Xunit;

// Os testes compartilham o mesmo banco isolado e inicializam migrations na aplicação.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
