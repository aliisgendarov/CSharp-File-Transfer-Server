using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("              FILE SERVER               ");
            Console.WriteLine("========================================");

            var ipAddress = IPAddress.Parse("127.0.0.1");
            var port = 5000;
            var ep = new IPEndPoint(ipAddress, port);

            string savePath = @"C:\Users\user\Desktop\ReceivedFiles";
            Directory.CreateDirectory(savePath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Server ise salindi...");
            Console.ResetColor();

            Console.WriteLine($"Port: {port}");
            Console.WriteLine("Gelen fayllar qebul edilir.");

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(ep);
                socket.Listen(1);

                while (true)
                {
                    Socket client = await socket.AcceptAsync();

                    _ = Task.Run(async () =>
                    {
                        using (client)
                        {
                            while (true)
                            {
                                try
                                {
                                    // Faylin adinin uzunlugu
                                    byte[] nameLengthBuffer = new byte[4]; // Burada 4 adin uzunluqu deyil gonderilen int i qebul etmek ucun ayrilmis olcudur

                                    if (!await ReceiveAllAsync(client, nameLengthBuffer))
                                        return;

                                    int nameLength = BitConverter.ToInt32(nameLengthBuffer, 0);

                                    if (nameLength <= 0 || nameLength > 1024)
                                        return;

                                    // Faylin adi
                                    byte[] nameBuffer = new byte[nameLength];

                                    if (!await ReceiveAllAsync(client, nameBuffer))
                                        return;

                                    string fileName = Path.GetFileName(Encoding.UTF8.GetString(nameBuffer));

                                    if (string.IsNullOrWhiteSpace(fileName))
                                        return;

                                    // Faylin olcusu
                                    byte[] sizeBuffer = new byte[8];

                                    if (!await ReceiveAllAsync(client, sizeBuffer))
                                        return;

                                    long fileSize = BitConverter.ToInt64(sizeBuffer, 0);

                                    if (fileSize < 0 || fileSize > 1024L * 1024 * 1024)
                                        return;

                                    string fullPath = Path.Combine(savePath, fileName);

                                    // Eyni adli faylin yazilmasin
                                    int counter = 1;

                                    while (File.Exists(fullPath))
                                    {
                                        string name = Path.GetFileNameWithoutExtension(fileName);
                                        string extension = Path.GetExtension(fileName);

                                        fullPath = Path.Combine(savePath, $"{name}_{counter}{extension}");

                                        counter++;
                                    }

                                    byte[] buffer = new byte[8192];
                                    long received = 0;

                                    using (var file = new FileStream(fullPath, FileMode.CreateNew))
                                    {
                                        while (received < fileSize)
                                        {
                                            // Sonuncu hissede qalan melumat 8192 den az ola biler, bu zaman yalniz qalan hisseni goturek
                                            int count = (int)Math.Min(buffer.Length, fileSize - received);

                                            int length = await client.ReceiveAsync(buffer.AsMemory(0, count));

                                            if (length == 0)
                                                throw new IOException("Fayl tam qebul edilmedi.");

                                            await file.WriteAsync(buffer.AsMemory(0, length));

                                            received += length;
                                        }
                                    }

                                    FileInfo info = new FileInfo(fullPath);

                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write("[QEBUL EDILDI] ");
                                    Console.ResetColor();

                                    Console.WriteLine($"{info.Name}  ({info.Length / 1024.0:F1} KB)  {DateTime.Now:HH:mm:ss}");

                                    await SendMessageAsync(client, "1|Fayl ugurla gonderildi!");
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Xeta: {ex.Message}");
                                    Console.ResetColor();

                                    try
                                    {
                                        await SendMessageAsync(client, "0|Fayl qebul edile bilmedi.");
                                    }
                                    catch (Exception)
                                    {

                                        throw;
                                    }
                                }
                            }

                        }
                    });
                }
            }
        }

        static async Task<bool> ReceiveAllAsync(Socket socket, byte[] buffer)
        {
            int received = 0;

            while (received < buffer.Length)
            {
                int length = await socket.ReceiveAsync(buffer.AsMemory(received));

                if (length == 0)
                    return false;

                received += length;
            }

            return true;
        }

        static async Task SendMessageAsync(Socket socket, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            int sent = 0;

            while (sent < data.Length)
            {
                int length = await socket.SendAsync(data.AsMemory(sent));

                if (length == 0)
                    throw new IOException("Mesaj gonderile bilmedi.");

                sent += length;
            }
        }
    }
}
