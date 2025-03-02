using System;
using System.IO;
using System.Text;

namespace Potato.ImageFlasher.CLI
{
    class Program
    {
        static ImageFlasher flasher;
        static string portName;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "KirinBootstrapper-by.Natsume Takashi";
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: KirinBootstrapper <port> [<file> <address> <sendTailFrame>]");
                return;
            }

            portName = args[0];

            flasher = new ImageFlasher();

            try
            {
                Console.WriteLine($"Opening port {portName}...");
                flasher.Open(portName);
                Console.WriteLine($"Port {portName} opened successfully.");

                if (args.Length == 1)
                {
                    Console.WriteLine("Enter the image file path and address to flash, or type 'exit' to quit.");
                    while (true)
                    {
                        Console.Write("Enter command: ");
                        string command = Console.ReadLine().Trim();

                        if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        string[] parts = command.Split(' ');

                        if (parts.Length < 2)
                        {
                            Console.WriteLine("Error: Invalid command. Usage: <file> <address> <sendTailFrame>");
                            continue;
                        }

                        string filePath = parts[0];
                        if (!File.Exists(filePath))
                        {
                            Console.WriteLine($"Error: File '{filePath}' not found.");
                            continue;
                        }

                        string addressString = parts[1];
                        if (!TryParseAddress(addressString, out int address))
                        {
                            Console.WriteLine($"Error: Invalid address '{addressString}'. Address must be a valid hexadecimal or decimal integer.");
                            continue;
                        }

 
                        bool sendTailFrame = parts.Length > 2 ? bool.TryParse(parts[2], out var result) && result : true;

                        try
                        {
                            Console.WriteLine($"Sending file '{filePath}' to address 0x{address:X8}...");
                            flasher.Write(filePath, address, sendTailFrame, progress =>
                            {

                                int progressLength = 50; 
                                int progressCount = (int)(progress * progressLength / 100); 
                                string progressBar = new string('-', progressCount) + new string(' ', progressLength - progressCount);
 
                                Console.Write($"\r[{progressBar}] {progress}%");
                            });

                            Console.WriteLine("\nSending completed successfully.");
                        }
                        catch (DeviceResponseException ex)
                        {
                            Console.WriteLine($"Device Response Error: {ex.Message}");
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine($"IO Error: {ex.Message}");
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            Console.WriteLine($"Port Access Error: {ex.Message}. Please check if the port is being used by another program or if you have the necessary permissions.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Unexpected Error: {ex.Message}");
                        }
                    }
                }

                else if (args.Length == 3 || args.Length == 4)
                {
                    string filePath = args[1];
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine($"Error: File '{filePath}' not found.");
                        return;
                    }

                    string addressString = args[2];
                    if (!TryParseAddress(addressString, out int address))
                    {
                        Console.WriteLine($"Error: Invalid address '{addressString}'. Address must be a valid hexadecimal or decimal integer.");
                        return;
                    }


                    bool sendTailFrame = args.Length == 4 ? bool.TryParse(args[3], out var result) && result : true;

                    try
                    {
                        Console.WriteLine($"Sending file '{filePath}' to address 0x{address:X8}...");
                        flasher.Write(filePath, address, sendTailFrame, progress =>
                        {

                            int progressLength = 100; 
                            int progressCount = (int)(progress * progressLength / 100); 
                            string progressBar = new string('-', progressCount) + new string(' ', progressLength - progressCount);

                            Console.Write($"\r[{progressBar}] {progress}%");
                        });

                        Console.WriteLine("\nSending completed successfully.");
                    }
                    catch (DeviceResponseException ex)
                    {
                        Console.WriteLine($"Device Response Error: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"IO Error: {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.WriteLine($"Port Access Error: {ex.Message}. Please check if the port is being used by another program or if you have the necessary permissions.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected Error: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Error: Too many parameters. Usage: ImageFlasher <port> [<file> <address> <sendTailFrame>]");
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: Access denied to the specified port. Ensure you have the required permissions.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Closing port...");
                flasher.Close();
                Console.WriteLine("Port closed.");
            }
        }


        static bool TryParseAddress(string addressString, out int address)
        {

            if (!addressString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                addressString = "0x" + addressString;
            }


            return int.TryParse(addressString.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out address);
        }
    }


    public class DeviceResponseException : Exception
    {
        public DeviceResponseException(string message) : base(message) { }
    }
}
