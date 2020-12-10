using System;
using System.Net.Sockets;

namespace TcpClientApp.Classes.TCP
{
    #region Interface iTcpClient
    /// <summary>
    /// Interface pour les événements de TcpClient
    /// </summary>
    public interface iTcpClient
    {
        void OnConnect(TcpClient client);
        void OnDisconnect(TcpClient client);
        void OnDataReceived(byte[] data, TcpClient client);
        void OnDataWritten(byte[] data, TcpClient client);
        void OnError(Exception exception, TcpClient client);
    }
    #endregion

    #region Classe TcpClient
    /// <summary>
    /// Classe permettant d'effectuer la connexion Tcp vers un serveur.
    /// </summary>
    public class TcpClient
    {
        #region Propriétés publiques
        /// <summary>
        /// Adresse IP du serveur.
        /// </summary>
        public string ServerIP { get { return _serverIP; } }
        /// <summary>
        /// Port du serveur.
        /// </summary>
        public int ServerPort { get { return _serverPort; } }
        /// <summary>
        /// Objet recevant les événements Tcp (connexion, déconnexion, données reçues, etc...).
        /// </summary>
        public iTcpClient ReferenceDelegate { get; set; }
        #endregion

        #region Propriétés privées
        byte[] _buffer = new byte[1024 * 1024];
        bool _connected = false;
        string _serverIP = "";
        int _serverPort = 0;
        System.Net.Sockets.TcpClient _tcpClient;
        
        #endregion

        #region Constructeurs
        public TcpClient(string serverIP, int port)
        {
            _serverIP = serverIP;
            _serverPort = port;

        }
        #endregion

        #region Methodes publiques
        /// <summary>
        /// Connecte le client Tcp au serveur.
        /// </summary>
        public void Connect()
        {
            if (_tcpClient != null && _tcpClient.Connected == true)
            {
                _tcpClient.Close();
                _tcpClient = null;
            }

            _tcpClient = new System.Net.Sockets.TcpClient();
            _tcpClient.BeginConnect(_serverIP, _serverPort, this.OnConnect, this);
        }

        /// <summary>
        /// Déconnecte le client Tcp du serveur.
        /// </summary>
        public void Disconnect()
        {
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _connected = false;

                if (this.ReferenceDelegate != null)
                {
                    this.ReferenceDelegate.OnDisconnect(this);
                }
            }
        }

        /// <summary>
        /// Envoie des données du client vers le serveur.
        /// </summary>
        /// <param name="data">Données à envoyer</param>
        public void Send(byte[] data)
        {
            if (_tcpClient != null)
            {
                NetworkStream stream = (NetworkStream)_tcpClient.GetStream();
                if (stream != null)
                {
                    stream.BeginWrite(data, 0, (int)data.Length, this.OnDataWritten, data);
                }
            }
            
        }
        #endregion

        #region Méthodes privées

        #endregion

        #region Méthodes callbacks
        /// <summary>
        /// Methode callback appelée lorsque la connexion est effective.
        /// </summary>
        /// <param name="result"></param>
        private void OnConnect(IAsyncResult result)
        {
            try
            {
                _tcpClient.EndConnect(result);

                _connected = true;

                NetworkStream stream = _tcpClient.GetStream();
                if (stream != null)
                {
                    stream.BeginRead(_buffer, 0, _buffer.Length, OnDataReceived, this);

                    if (this.ReferenceDelegate != null)
                    {
                        this.ReferenceDelegate.OnConnect(this);
                    }
                }
            }
            catch (Exception e)
            {
                if (this.ReferenceDelegate != null)
                {
                    this.ReferenceDelegate.OnError(e, this);
                }
            }            
        }

        /// <summary>
        /// Methode callback appelée lorsque la connexion est interrompue.
        /// </summary>
        /// <param name="result"></param>
        private void OnDisconnect(IAsyncResult result)
        {
            try
            {
                _tcpClient.EndConnect(result);

                NetworkStream stream = _tcpClient.GetStream();
                if (stream != null)
                {
                    stream.BeginRead(_buffer, 0, _buffer.Length, OnDataReceived, this);

                    if (this.ReferenceDelegate != null)
                    {
                        this.ReferenceDelegate.OnConnect(this);
                    }
                }
            }
            catch (Exception e)
            {
                if (this.ReferenceDelegate != null)
                {
                    this.ReferenceDelegate.OnError(e, this);
                }
            }
            
        }

        /// <summary>
        /// Méthode callback appelée lorsque des données sont reçues.
        /// </summary>
        /// <param name="result"></param>
        private void OnDataReceived(IAsyncResult result)
        {
            try
            {
                if (_connected == true)
                {
                    NetworkStream stream = _tcpClient.GetStream();
                    if (stream != null)
                    {
                        int nLen = stream.EndRead(result);

                        if (nLen <= 0)
                        {
                            // Erreur: on ferme.
                            this.Disconnect();
                        }
                        else
                        {
                            if (this.ReferenceDelegate != null)
                            {
                                byte[] tmpBuffer = new byte[nLen];
                                Buffer.BlockCopy(_buffer, 0, tmpBuffer, 0, nLen);
                                this.ReferenceDelegate.OnDataReceived(tmpBuffer, this);
                            }

                            stream.BeginRead(_buffer, 0, _buffer.Length, OnDataReceived, this);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (this.ReferenceDelegate != null)
                {
                    this.ReferenceDelegate.OnError(e, this);
                }
            }
            
        }

        /// <summary>
        /// Méthode callback appelée lorsque des données ont été écrite vers le serveur.
        /// </summary>
        /// <param name="result"></param>
        private void OnDataWritten(IAsyncResult result)
        {
            if (_tcpClient != null)
            {
                NetworkStream stream = (NetworkStream)_tcpClient.GetStream();
                if (stream != null)
                {
                    stream.EndWrite(result);  // Finalise l'opération d'écriture asynchrone.

                    byte[] data = (byte[])result.AsyncState;
                    if (this.ReferenceDelegate != null)
                    {
                        this.ReferenceDelegate.OnDataWritten(data, this);
                    }
                }
            }
        }
        #endregion
    }

    #endregion
}
