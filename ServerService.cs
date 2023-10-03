using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DemoServer {
    class ServerService : IDisposable {
        private const int BufferSize = 65536;

        private readonly TcpListener _listener;
        private readonly IPAddress _ip;
        private readonly int _port;

        public ServerService(IPAddress? ip, int port = 1111) {
            ip ??= IPAddress.Any;
            _ip = ip;
            _port = port;
            _listener = new TcpListener(ip, port);
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken) {
            _listener.Start();
            Console.WriteLine($"Server listening on {_ip}:{_port}");

            while (!stoppingToken.IsCancellationRequested) {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(client, stoppingToken);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken) {
            NetWorker? parser = null;
            var ep = client.Client.RemoteEndPoint;
            try {
                Console.WriteLine($"Client connected: {ep}");

                await using var stream = client.GetStream();
                var headerBuffer = new byte[2];

                parser = new NetWorker(stream);

                // Check client
                async Task AliveChecker(NetWorker p, TcpClient client2) {
                    await Task.Delay(1000, cancellationToken);
                    if (client2.Connected && p is { HasAlive: true }) {
                        _ = AliveChecker(p, client2);
                    } else {
                        client2.Close();
                    }
                }
                _ = AliveChecker(parser, client);

                //var test = new byte[8];
                //await stream.ReadAsync(test, 0, test.Length, cancellationToken);

                while (await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length, cancellationToken) > 0) {
                    var packetHead = BitConverter.ToInt16(headerBuffer, 0);
                    if (packetHead != 0x6529) {
                        // Invalid packet
                        break;
                    }

                    var lengthBuffer = new byte[4];
                    if (await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length, cancellationToken) == 0) {
                        // Client disconnected
                        break;
                    }

                    var packetLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (packetLength > 64 * 1024 * 1024) break; // Too large packet

                    var dataBuffer = new byte[packetLength];

                    var bytesReadTotal = 0;
                    while (bytesReadTotal < packetLength) {
                        var bytesToRead = Math.Min(dataBuffer.Length - bytesReadTotal, BufferSize);
                        var bytesReadPartial = await stream.ReadAsync(dataBuffer, bytesReadTotal, bytesToRead, cancellationToken);
                        if (bytesReadPartial == 0) {
                            // Client disconnected
                            break;
                        }
                        bytesReadTotal += bytesReadPartial;
                    }

                    if (bytesReadTotal != packetLength) continue; // Invalid total data

                    // Process data
                    await parser.ParseData(dataBuffer);
                }
            } catch (OperationCanceledException) {
                // Ignore cancellation
            } catch (Exception ex) {
                if (ex.InnerException is SocketException { ErrorCode: 10054 }) {
                    Console.WriteLine("Client called force disconnect");
                    return;
                }
                Console.WriteLine(ex.InnerException is SocketException { ErrorCode: 995 }
                    ? "Alive checker disconnect client"
                    : $"Error handling client: {ex}");
            } finally {
                parser?.Dispose();
                client.Dispose();
                Console.WriteLine($"Client disconnected: {ep}");
            }
        }

        public void Dispose() {
            _listener.Stop();
        }
    }
}
