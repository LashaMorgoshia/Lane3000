 /*
     * 
     * START_TRANSACTION
     * CANCEL_TRANSACTION
     * GET_TRANSACTION_STATUS
     * GET_TERMINAL_STATUS
     * PRINT_RECEIPT
     * UPDATE_FIRMWARE
     * REBOOT_TERMINAL
     * GET_CONFIGURATION
     * SET_CONFIGURATION
     * 
     */

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


                    //// Example: Start a transaction
                    //SendCommand(serialPort, "START_TRANSACTION");

                    //// Example: Get transaction status
                    //SendCommand(serialPort, "GET_TRANSACTION_STATUS");

                    //// Example: Cancel transaction
                    //SendCommand(serialPort, "CANCEL_TRANSACTION");
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

    static void SendCommand(SerialPort serialPort, string command)
    {
        byte[] commandBytes = Encoding.ASCII.GetBytes(command + "\r\n");
        serialPort.Write(commandBytes, 0, commandBytes.Length);
        Console.WriteLine($"Command '{command}' sent.");

        // Read response from the terminal
        byte[] buffer = new byte[256];
        int bytesRead = serialPort.Read(buffer, 0, buffer.Length);

        string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Console.WriteLine("Response from terminal: " + response);
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
