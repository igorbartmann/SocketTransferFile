using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Application
{
    public class Client
    {
        public static async Task Main(string[] args)
        {
            #region Constants
            const int SERVER_PORT = 11000;
            const string RECEIVED_FILES_DIRECTORY_PATH = @"C:\Users\ibartmann\OneDrive - GVDASA\Área de Trabalho\Faculdade\Redes de Computadores\Socket_Communication\Client\Application\ReceivedFiles";
            const string GET_FILES_LIST_COMMAND = "Get_Files";
            const string GET_FILE_ID_COMMAND = "Get_File_";
            #endregion

            // Inicializando IpEndPoint (par endereço e porta).
            var ipEndPoint = await GetIpEndPoint(SERVER_PORT);

            // Executa até que seja cancelado pelo usuário.
            bool run = true;
            while (run)
            {
                // Cria o socket.
                using (var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    // Conecta com o servidor.
                    await socket.ConnectAsync(ipEndPoint);

                    // Imprime mensagem alertando sobre a conexão ter sido estabelecida.
                    Console.WriteLine("Socket connection established...");

                    // Imprime menu.
                    Console.WriteLine(@$"
                        ***************************************************
                                                MENU
                        [1] - Obter Arquivos disponíveis.
                        [2] - Obter Arquivo.
                        [3] - Sair.
                        ***************************************************");

                    // Solicita opção do menu desejada.
                    Console.Write("Digite a opção desejada:");
                    var optionString = Console.ReadLine();
                    if (Int32.TryParse(optionString, out int option) && option >= 1 && option <=3)
                    {
                        // Trata opção selecionada.
                        switch (option)
                        {
                            case 1: // Solicita lista de arquivos para o servidor.
                                var commandFileListBytes = Encoding.UTF8.GetBytes(GET_FILES_LIST_COMMAND);
                                await socket.SendAsync(commandFileListBytes, SocketFlags.None);
                                Console.WriteLine("Socket client sent request: \"GET_FILES_LIST_COMMAND\"");

                                var responseBytes = new byte[1024];
                                var responseLength = await socket.ReceiveAsync(responseBytes, SocketFlags.None);
                                var response = Encoding.UTF8.GetString(responseBytes, 0, responseLength);

                                Console.WriteLine($"Files available on the server: \n{response}");
                                break;
                            case 2: // Solicita arquivo específico para o servidor.
                                Console.Write("Digite o código identificador (Id) do arquivo a ser obtido no servidor: ");

                                var fileIdString = Console.ReadLine();
                                if (Int32.TryParse(fileIdString, out int fileId))
                                {
                                    var commandFileIdString = GET_FILE_ID_COMMAND + fileId;
                                    var commandFileIdBytes = Encoding.UTF8.GetBytes(commandFileIdString);
                                    await socket.SendAsync(commandFileIdBytes, SocketFlags.None);
                                    Console.WriteLine("Socket client sent request: \"GET_FILE_ID_COMMAND\"");

                                    var responseFileBytes = new byte[1024 * 50000];
                                    var responseFileLength = socket.Receive(responseFileBytes, SocketFlags.None);
                                    if (responseFileLength > 0)
                                    {
                                        var fileNameLength = BitConverter.ToInt16(responseFileBytes, 0);
                                        var fileName = Encoding.UTF8.GetString(responseFileBytes, 4, fileNameLength);

                                        var filePath = Path.Combine(RECEIVED_FILES_DIRECTORY_PATH, fileName);
                                        BinaryWriter binaryWriter = new BinaryWriter(File.Open(filePath, FileMode.Create));
                                        binaryWriter.Write(responseFileBytes, 4 + fileNameLength, responseFileLength - 4 - fileNameLength);

                                        while (responseFileLength > 0)
                                        {
                                            responseFileLength = socket.Receive(responseFileBytes, responseFileBytes.Length, SocketFlags.None);
                                            if (responseFileLength == 0)
                                            {
                                                binaryWriter.Close();
                                            }
                                            else
                                            {
                                                binaryWriter.Write(responseFileBytes, 0, responseFileLength);
                                            }
                                        }

                                        Console.WriteLine($"Socket client received the file as response and stored it in {filePath}!");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Erro ao consultar o arquivo!");
                                    }
                                }
                                else 
                                {
                                    Console.WriteLine("Erro, id inválido (deve ser digitado um número)!");
                                }
                                break;
                            case 3: // Encerra a execução.
                                run = false;
                                break;
                        }
                    }
                    else 
                    {
                        Console.WriteLine("Erro, opção inválida!");
                    }

                    // Finaliza o socket cliente.
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            }

            // Imprime mensagem de encerramento da aplicação cliente.
            Console.WriteLine("Client application finished!");
        }

        /// <summary>
        /// Obter IPEndPoint.
        /// </summary>
        /// <param name="port">Número da porta ao qual o servidor estará atrelado.</param>
        /// <returns>Objeto do tipo IPEndPoint.</returns>
        private static async Task<IPEndPoint> GetIpEndPoint(int port)
        {
            IPHostEntry ipHostEntry = await Dns.GetHostEntryAsync(Dns.GetHostName());
            IPAddress ipAddress = ipHostEntry.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

            return ipEndPoint;
        }
    }
}