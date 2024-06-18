using System;
using System.IO.Ports;
using System.Text;

namespace Lane3000Integration
{
    class Program
    {
        static void Main(string[] args)
        {
            // Replace with the actual COM port used by the Lane 3000 terminal
            string portName = "COM7";
            int baudRate = 115200;

            try
            {
                using (SerialPort serialPort = new SerialPort(portName, baudRate))
                {
                    serialPort.Open();

                    if (serialPort.IsOpen)
                    {
                        Console.WriteLine("Connected to Lane 3000 terminal.");

                        // Example: Start a payment with a specified amount
                        SendPaymentAmount(serialPort, 50.00); // Sending $50.00 as an example

                        // Read response (this is just an example, actual implementation may vary)
                        string response = ReadResponse(serialPort);
                        Console.WriteLine("Response from terminal: " + response);
                    }
                    else
                    {
                        Console.WriteLine("Failed to open serial port.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static void SendPaymentAmount(SerialPort serialPort, double amount)
        {
            // Construct the payment command
            string command = $"START_PAYMENT:{amount:F2}"; // Hypothetical command format
            byte[] commandBytes = Encoding.ASCII.GetBytes(command + "\r\n");

            // Send the command
            serialPort.Write(commandBytes, 0, commandBytes.Length);
            Console.WriteLine($"Payment command '{command}' sent.");
        }

        static string ReadResponse(SerialPort serialPort)
        {
            byte[] buffer = new byte[256];
            int bytesRead = serialPort.Read(buffer, 0, buffer.Length);

            return Encoding.ASCII.GetString(buffer, 0, bytesRead);
        }
    }
}
