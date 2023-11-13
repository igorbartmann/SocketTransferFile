using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Application
{
    public class Server
    {
        public static async Task Main(string[] args)
        {
            #region Constants
            const int SERVER_PORT = 11000;
            const int SERVER_PENDING_CONNECTIONS_QUEUE_LENGTH = 100;
            const string FILES_DIRECTORY_PATH = @"C:\Users\ibartmann\OneDrive - GVDASA\Área de Trabalho\Faculdade\Redes de Computadores\Socket_Communication\Server\Application\Files";
            const string GET_FILES_LIST_COMMAND = "Get_Files";
            const string GET_FILE_ID_COMMAND = "Get_File_";
            #endregion

            // Inicializando lista dos arquivos armazenados no servidor.
            var fileList = GetFilesStored(FILES_DIRECTORY_PATH);

            // Inicializando IpEndPoint (par endereço e porta).
            var ipEndPoint = await GetIpEndPoint(SERVER_PORT);

            // Criando socket.
            using (var serverSocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                // Vinculando socket criado com o IpEndPoint (par endereço e porta).
                serverSocket.Bind(ipEndPoint);

                // Definindo tamanho máximo da fila de espera.
                serverSocket.Listen(SERVER_PENDING_CONNECTIONS_QUEUE_LENGTH);

                // Imprime mensagem alertando sobre o socket servidor estar disponível.
                Console.WriteLine("Socket server available...");

                // Socket server roda até que a operação seja cancelada.
                while (true)
                {
                    // Aguarda conexão com cliente.
                    var clientSocket = await serverSocket.AcceptAsync();
                    
                    // Cria buffer para armazenar bytes recebidos.
                    var receivedRequestBytes = new byte[1024];

                    // Aguarda recebimento de dados da conexão via socket com o cliente.
                    var receivedRequestLength = await clientSocket.ReceiveAsync(receivedRequestBytes, SocketFlags.None);

                    // Converte os bytes recebidos para string.
                    var receivedRequest = Encoding.UTF8.GetString(receivedRequestBytes, 0, receivedRequestLength);

                    // Imprime a mensagem recebida do cliente.
                    Console.WriteLine($"Socket server received the request: \"{receivedRequest}\"");

                    // Trata o comando recebido.
                    byte[]? response = null;
                    if (receivedRequest.Contains(GET_FILES_LIST_COMMAND, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var stringBuilder = new StringBuilder();
                        foreach (var file in fileList)
                        {
                            stringBuilder.AppendLine($"FileId: ({file.Id}) - FileName: \"{file.Name}\"\n");
                        }

                        response = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                    }
                    else if (receivedRequest.Contains(GET_FILE_ID_COMMAND, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var fileIdString = receivedRequest.Replace(GET_FILE_ID_COMMAND, string.Empty);
                        if (Int32.TryParse(fileIdString, out int fileId))
                        {
                            var archive = fileList.FirstOrDefault(file => file.Id == fileId);
                            if (archive is not null)
                            {
                                var fileNameBytes = Encoding.UTF8.GetBytes(archive.Name);
                                var fileNameBytesLengthInBytes = BitConverter.GetBytes(fileNameBytes.Length);
                                var fileBytes = File.ReadAllBytes(archive.Path);
                                
                                response = new byte[4 + fileNameBytes.Length + fileBytes.Length];
                                fileNameBytesLengthInBytes.CopyTo(response, 0);
                                fileNameBytes.CopyTo(response, 4);
                                fileBytes.CopyTo(response, 4 + fileNameBytes.Length);
                            }
                            else 
                            {
                                response = Encoding.UTF8.GetBytes(string.Empty);
                            }
                        }
                    }
                    else
                    {
                        response = Encoding.UTF8.GetBytes("Command not recognized!");
                    }

                    if (response is not null)
                    {
                        // Enviado resposta ao comando recebido.
                        await clientSocket.SendAsync(response, SocketFlags.None);
                            
                        // Imprime mensagem alertando que o socket servidor responder.
                        Console.WriteLine("Socket server sent response!");
                    }
                    else 
                    {
                        throw new ApplicationException("An error has ocurred!");
                    }                    

                    // Finaliza o socket de comunicação com o cliente.
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }
        }

        /// <summary>
        /// Carrega os arquivos armazenados no servidor.
        /// </summary>
        /// <param name="path">Caminho do diretório dos arquivos.</param>
        /// <returns>Lista com os arquivos armazenados no servidor.</returns>
        private static IList<Archive> GetFilesStored(string path)
        {
            // Inicializando lista dos arquivos armazenados no servidor.
            var filesNameWithPath = Directory.GetFiles(path);

            int currentFileId = 0;
            var fileList = new List<Archive>();
            foreach (var fileNameWithPath in filesNameWithPath)
            {
                fileList.Add(new Archive(++currentFileId, Path.GetFileName(fileNameWithPath), fileNameWithPath));
            }

            return fileList;
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

    public class Archive
    {
        public Archive(int id, string name, string path)
        {
            Id = id;
            Name = name;
            Path = path;
        }

        public int Id {get;set;}
        public string Name {get;set;}
        public string Path {get;set;}
    }
}