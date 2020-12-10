using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Classes.TCP
{
    #region Inteface delegate TCPServer
    /// <summary>
    /// Interface pour l'objet qui recevra les événements données, connexion entrante, déconnexion.
    /// </summary>
    public interface iTCPServerDelegate
    {
        void OnConnect(TcpServer server, TCPServerClient client);
        void OnDisconnect(TcpServer server, TCPServerClient client);
        void onTcpReading(byte[] buffer, TCPServerClient client);
        void onTcpSent(byte[] buffer, TCPServerClient client);
    }
    #endregion

    #region Structure utile
    struct TcpWrittenData
    {
        public byte[] Data { get; set; }
        public TCPServerClient Client { get; set; }

        public TcpWrittenData(byte[] data, TCPServerClient client)
        {
            this.Data = data;
            this.Client = client;
        }
    }
    #endregion

    #region Classe Serveur TCP
    /// <summary>
    /// Classe représentant le serveur TCP sur l'ordinateur.
    /// Ce serveur est multi-clients (plusieurs connexions simultanées sur le même port).
    /// </summary>
    public class TcpServer
    {
        #region Propriétés publiques
        public List<TCPServerClient> Clients { get; set; }
        public Exception CurrentException { get { return _currentException;  } }
        public byte[] ReadBuffer { get; set; }
        public int Port { get; set; }
        public iTCPServerDelegate ReferenceDelegate { get; set; }
        public NetworkStream Stream { get; set; }

        #endregion

        #region Propriétés protégées
        protected TcpListener _tcpListener = null;
        #endregion

        #region Propriétés privées
        private Exception _currentException = null;
        #endregion

        #region Constructeurs
        public TcpServer(int port, iTCPServerDelegate refDelegate)
        {
            this.Port = port;
            this.ReferenceDelegate = refDelegate;
            this.Clients = new List<TCPServerClient>();
        }
        #endregion

        #region Méthodes publiques
        /// <summary>
        /// Ecrit des données sur le flux réseau vers un client.
        /// </summary>
        /// <param name="dataToSend">Données à envoyer</param>
        /// <param name="client">Client vers lequel envoyer les données</param>
        public void Send(byte[] dataToSend, TCPServerClient client)
        {
            if (client != null && client.Stream != null && client.Stream.CanWrite)
            {
                client.Stream.BeginWrite(dataToSend, 0, dataToSend.Length, this.OnMessageSentCallback, new TcpWrittenData(dataToSend, client));
            }
        }

        /// <summary>
        /// Ferme la connexion avec le client dont l'ip est passée en paramètre.
        /// </summary>
        /// <param name="ip">adresse IP du client à supprimer (ex: "192.168.2.23")</param>
        public void RemoveClientsWithIp(string ip)
        {
            if (this.Clients != null)
            {
                for (int i = this.Clients.Count - 1; i >= 0; i--)
                {
                    if (("" + this.Clients[i].IpAddress).Trim() == ("" + ip).Trim())
                    {
                        this.Clients[i].Disconnect();
                        this.Clients.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Démarre l'écoute réseau du serveur
        /// </summary>
        public void Start()
        {
            if (this.Port <= 0)
            {
                _currentException = new Exception("Le port n'est pas spécifié");
            }
            else
            {
                if (_tcpListener != null && this.Clients.Count > 0)
                {
                    this.Stop();
                }

                _tcpListener = new TcpListener(IPAddress.Any, this.Port);

                // Démarre l'écoute TCP :
                _tcpListener.Start();

                // Lance la fonction d'attente de connexion de manière asynchrone:
                _tcpListener.BeginAcceptSocket(new AsyncCallback(this.OnAcceptSocket), this);
            }
        }

        /// <summary>
        /// Stoppe l'écoute et déconnecte tous les clients.
        /// </summary>
        public void Stop()
        {
            // Déconnecte chaque client:
            for (int i = this.Clients.Count - 1; i >= 0; i--)
            {
                TCPServerClient c = this.Clients[i];
                c.Disconnect();
            }

            // Supprime l'objet TcpListener:
            if (_tcpListener != null)
            {
                _tcpListener.Stop();
                _tcpListener = null;
            }
        }
        #endregion

        #region Méthodes Callbacks
        /// <summary>
        /// Tentative de connexion au serveur par un client
        /// </summary>
        /// <param name="result">Résultat de l'opération asynchrone de connexion (contenient l'objet serveur)</param>
        public void OnAcceptSocket(IAsyncResult result)
        {
            TcpServer server = (TcpServer)result.AsyncState;
            TcpListener listener = server._tcpListener;

            try
            {
                if (listener != null)
                {
                    Socket tmpSocket = listener.EndAcceptSocket(result);  // finalisation de l'opération asynchrone.

                    // Création du client TCP:
                    TCPServerClient client = new TCPServerClient(tmpSocket, server);
                    this.Clients.Add(client);

                    // Si le delegate existe, on appelle sa methode OnConnect:
                    if (this.ReferenceDelegate != null)
                    {
                        this.ReferenceDelegate.OnConnect(this, client);
                    }

                    // On relane l'acceptation asynchrone du prochain client (connexion multi-clients):
                    listener.BeginAcceptSocket(new AsyncCallback(server.OnAcceptSocket), server);
                }
            }
            catch (Exception e)
            {
                _currentException = e;
            }
        }

        /// <summary>
        /// Un client vient de se déconnecter.
        /// </summary>
        /// <param name="client">Client venant de se déconnecter</param>
        public void OnDisconnect(TCPServerClient client)
        {
            if (this.ReferenceDelegate != null)
            {
                this.ReferenceDelegate.OnDisconnect(this, client);
            }
        }

        /// <summary>
        /// Les données ont fini d'être envoyées au client.
        /// </summary>
        /// <param name="result">Objet de résultat asynchrone contenant l'objet client.</param>
        private void OnMessageSentCallback(IAsyncResult result)
        {
            TcpWrittenData writtenData = (TcpWrittenData)result.AsyncState;
            if (writtenData.Client != null)
            {
                writtenData.Client.Stream.EndWrite(result); // Finalisation de l'opération asynchrone.

                if (this.ReferenceDelegate != null)
                {
                    this.ReferenceDelegate.onTcpSent(writtenData.Data, writtenData.Client);
                }
            }
        }

        #endregion
    }
    #endregion

    #region Classe client (du serveur)
    /// <summary>
    /// Classe permettant de représenter un client du serveur.
    /// </summary>
    public class TCPServerClient
    {
        #region Membres publiques
        public Exception CurrentException { get { return _currentException; } }
        public string IpAddress { get; set; }
        public bool IsConnected { get { return (_socket == null) ? false : _socket.Connected; } }
        public byte[] ReadBuffer = new byte[1024 * 1024];   // buffer de données.
        public TcpServer ServerParent { get; set; } // objet serveur.
        public NetworkStream Stream { get; set; }  // flux de données réseau.
        #endregion

        #region Membres protégés
        protected Exception _currentException = null;
        #endregion

        #region Membres privés
        private Socket _socket = null;
        #endregion

        #region Constructeurs
        /// <summary>
        /// Constructeur unique de la classe
        /// </summary>
        /// <param name="socket">Objet socket contenant la connexion sous-jacente.</param>
        /// <param name="server">Serveur ayant acceptéla connexion.</param>
        public TCPServerClient(Socket socket, TcpServer server)
        {
            try
            {
                // Le serveur vient de créer son client qui nous permettra
                this.IpAddress = socket.RemoteEndPoint.ToString();
                this.ServerParent = server;

                _socket = socket;

                this.Stream = new NetworkStream(socket);
                this.Stream.ReadTimeout = System.Threading.Timeout.Infinite;
            }
            catch (System.IO.IOException exception)
            {
                _currentException = exception;
            }

            if (this.Stream.CanRead == true)
            {
                this.Stream.BeginRead(this.ReadBuffer, 0, this.ReadBuffer.Length, new AsyncCallback(OnBeginReceive), this);
            }
            else
            {
                _currentException = new Exception("Impossible de lire sur ce flux de données");
            }
        }
        #endregion

        #region Méthodes publiques
        /// <summary>
        /// Déconnexion du client par le poste serveur.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                // Fermeture du flux:
                if (this.Stream != null)
                {
                    this.Stream.Close();
                    this.Stream = null;
                }

                // Déconnexion du socket:
                if (_socket != null)
                {
                    _socket.Disconnect(true);
                    _socket.Dispose();
                }

                // Génération de l'évéenemnt de déconnexion pour le delegate:
                if (this.ServerParent != null && this.ServerParent.Clients != null)
                {
                    this.ServerParent.Clients.Remove(this);
                }
            }
            catch (Exception ex)
            {
                _currentException = ex;
            }
        }
        #endregion

        #region Evénements réseaux
        /// <summary>
        /// Methode callback statique se déclenchant quand un client commence à recevoir des données. 
        /// Le client est passé dans le paramètre "Result".
        /// </summary>
        /// <param name="result">Objet de méthode asynchrone contenant le client ayant reçu des données</param>
        static void OnBeginReceive(IAsyncResult result)
        {
            TCPServerClient client = (TCPServerClient)result.AsyncState;
            try
            {
                if (client != null)
                {
                    // Obtiention du flux réseau du client
                    NetworkStream stream = client.Stream;

                    if (stream != null)
                    {
                        int nLen = stream.EndRead(result);      // finalise la lecture asynchrone.
                        if (nLen <= 0)
                        {
                            // Si longueur inférieur ou égale à 0 : erreur dans le flux => on déconnecte.
                            client.Disconnect();
                            if (client.ServerParent != null)
                            {
                                client.ServerParent.OnDisconnect(client);
                            }
                        }
                        else
                        {
                            // Création du buffer de données à l abonne longueur:
                            byte[] tmpBuffer = new byte[nLen];

                            // Copie de la bonne quantité de données du buffer du client vers tmpBuffer:
                            Buffer.BlockCopy(client.ReadBuffer, 0, tmpBuffer, 0, nLen);

                            // On relance la lecture pour obtenir les prochaines données:
                            stream.BeginRead(client.ReadBuffer, 0, client.ReadBuffer.Length, new AsyncCallback(OnBeginReceive), client);

                            if (client.ServerParent != null && client.ServerParent.ReferenceDelegate != null)
                            {
                                // Génération de l'évéenement de lecture sur le delegate:
                                client.ServerParent.ReferenceDelegate.onTcpReading(tmpBuffer, client);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (client != null)
                {
                    client._currentException = ex;
                }
            }
        }
        #endregion

    }
    #endregion

}
