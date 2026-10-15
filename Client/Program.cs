using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("              FILE CLIENT               ");
            Console.WriteLine("========================================");

            var ipAddress = IPAddress.Parse("127.0.0.1");
            var port = 5000;
            var ep = new IPEndPoint(ipAddress, port);

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    await socket.ConnectAsync(ep);

                    while (true)
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("1 - ");
                        Console.ResetColor();
                        Console.WriteLine("Fayl gonder (sekil/ses)");

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("0 - ");
                        Console.ResetColor();
                        Console.WriteLine("Cixis");

                        Console.WriteLine("========================================");
                        Console.Write("Secim edin: ");

                        if (!int.TryParse(Console.ReadLine(), out int choice))
                        {
                            Console.WriteLine("Duzgun secim daxil edin!");
                            continue;
                        }

                        if (choice == 0)
                        {
                            Console.WriteLine("Cixis Edilir...");
                            break;
                        }

                        if (choice != 1)
                        {
                            Console.WriteLine("Bele bir secim yoxdur!");
                            continue;
                        }

                        Console.WriteLine("\nFaylin yolunu daxil edin:");
                        Console.ForegroundColor = ConsoleColor.Yellow;

                        string? filePath = Console.ReadLine();

                        Console.ResetColor();

                        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Fayl yolu yanlisdir ve ya fayl movcud deyil!");
                            Console.ResetColor();

                            continue;
                        }

                        try
                        {
                            FileInfo fileInfo = new FileInfo(filePath);

                            string fileName = fileInfo.Name;
                            byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                            byte[] fileBytes = File.ReadAllBytes(filePath);

                            Console.WriteLine("Gonderilir...");

                            byte[] nameLengthBytes = BitConverter.GetBytes(nameBytes.Length);

                            await SendAllAsync(socket, nameLengthBytes);

                            await SendAllAsync(socket, nameBytes);

                            byte[] sizeBytes = BitConverter.GetBytes((long)fileBytes.Length);

                            await SendAllAsync(socket, sizeBytes);

                            await SendAllAsync(socket, fileBytes);

                            byte[] responseBuffer = new byte[1024];

                            int responseLength = await socket.ReceiveAsync(responseBuffer);

                            if (responseLength == 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Server cavab vermeden baglandi.");
                                Console.ResetColor();

                                break;
                            }

                            string response = Encoding.UTF8.GetString(responseBuffer, 0, responseLength);

                            string[] parts = response.Split('|', 2);

                            if (parts.Length == 2 && parts[0] == "1")
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine(parts[1]);
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;

                                Console.WriteLine(parts.Length == 2 ? parts[1] : "Server-den duzgun cavab alinmadi.");

                                Console.ResetColor();
                            }
                        }
                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SocketException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Xeta: {ex.Message}");
                            Console.ResetColor();

                            if (!socket.Connected)
                                break;
                        }
                    }
                }
                catch (SocketException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Server aciq deyil ve ya elaqe qurmaq mumkun olmadi.");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Xeta: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        static async Task SendAllAsync(Socket socket, byte[] data)
        {
            int sent = 0;

            while (sent < data.Length)
            {
                int length = await socket.SendAsync(data.AsMemory(sent));

                if (length == 0)
                {
                    throw new IOException("Melumat tam gonderile bilmedi.");
                }

                sent += length;
            }
        }
    }
}
