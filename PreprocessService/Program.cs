using PreprocessService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

var app = builder.Build();
app.MapGrpcService<PreprocessServiceImpl>();
app.MapGet("/", () => "Serviço de Pré-Processamento RPC (gRPC) está ativo.");
app.Run();
// O serviço de pré-processamento RPC (gRPC) está ativo e pronto para receber requisições.
// Certifique-se de que o cliente gRPC esteja configurado corretamente para se comunicar com este serviço.