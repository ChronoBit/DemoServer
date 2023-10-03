using System.Net;
using DemoServer;

using var server = new ServerService(IPAddress.Any, 1000);
await server.ExecuteAsync(CancellationToken.None);