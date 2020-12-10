using System;
using System.Text;
using System.Windows;

using TcpClientApp.Classes.TCP;

namespace TcpClientApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, iTcpClient
    {
        #region Propriétés publiques

        #endregion

        #region Propriétés publiques
        TcpClient _client = null;       // Objet client à connecter au serveur.
        #endregion

        #region Constructeur
        public MainWindow()
        {
            InitializeComponent();
            
        }
        #endregion

        #region Méthodes publiques

        #endregion

        #region Méthodes privées
        /// <summary>
        /// Ecrit le texte passé en argument dans la zone de texte du bas de la fenêtre.
        /// </summary>
        /// <param name="texte">Texte à écrire (sera suivi d'un retour chariot)</param>
        private void LogEcrire(string texte)
        {
            this.TxtLog.Text += texte + "\r\n";
        }

        /// <summary>
        /// Active ou non les boutons suivant si l'état est connecté ou pas.
        /// </summary>
        /// <param name="isConnected">True si connecté, False si déconnecté.</param>
        private void EnableButtons(bool isConnected)
        {
            this.BttnConnect.IsEnabled = !isConnected;
            this.BttnDisconnect.IsEnabled = isConnected;
            this.GrdMessage.IsEnabled = isConnected;
        }
        #endregion

        #region Méthodes interface utilisateur
        private void BttnConnect_Click(object sender, RoutedEventArgs e)
        {
            // Connecte au serveur:
            int port = Convert.ToInt32(this.TxtPort.Text);

            if (port > 0 && port < 655535)
            {
                _client = new TcpClient(this.TxtServer.Text, port);
                _client.ReferenceDelegate = this;
                _client.Connect();
            }
        }

        private void BttnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            // Déconnecte du serveur:
            if (_client != null)
            {
                _client.Disconnect();
            }
        }

        private void BttnSend_Click(object sender, RoutedEventArgs e)
        {
            // Envoie le texte de TxtMessage au serveur:
            if (_client != null)
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(this.TxtMessage.Text);
                if (data != null && data.Length > 0)
                {
                    _client.Send(data);
                }
            }
        }
        #endregion

        #region Evénements Tcp Client
        /// <summary>
        /// Evénement déclenché lorsque la connexion au serveur est établie.
        /// </summary>
        /// <param name="client">Client connecté</param>
        void iTcpClient.OnConnect(TcpClient client)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.LogEcrire("Connecté à [" + client.ServerIP + ":"+ client.ServerPort +"]");

                this.EnableButtons(true);

            }));
        }

        /// <summary>
        /// Evénement déclenché lorsque des données en provenance du serveur sont reçues.
        /// </summary>
        /// <param name="data">Tableau d'octets renvoyés par le serveur</param>
        /// <param name="client">Objet client ayant reçu les données</param>
        void iTcpClient.OnDataReceived(byte[] data, TcpClient client)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                string text = Encoding.UTF8.GetString(data);
                LogEcrire("Message de [" + client.ServerIP + "]: " + text);
            }));
        }

        /// <summary>
        /// Evénement déclenché lorsque des données ont été envoyées correctement vers le serveur.
        /// </summary>
        /// <param name="data">Tableau d'octets à envoyer au serveur</param>
        /// <param name="client">Objet TcpClient ayant effectué l'envoi</param>
        void iTcpClient.OnDataWritten(byte[] data, TcpClient client)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                string text = Encoding.UTF8.GetString(data);
                LogEcrire("["+text+"] => envoyé à [" + client.ServerIP + "]");
            }));
        }

        /// <summary>
        /// Evénement déclenché lorsque le client a été déconnecté du serveur.
        /// </summary>
        /// <param name="client">Objet TcpClient déconnecté</param>
        void iTcpClient.OnDisconnect(TcpClient client)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.LogEcrire("Déconnecté de [" + client.ServerIP + "]");

                this.EnableButtons(false);
            }));
            
        }

        /// <summary>
        /// Evénement déclenché lorsqu'une erreur intervient.
        /// </summary>
        /// <param name="exception">Erreur survenue</param>
        /// <param name="client">Objet TcpClient ayant rencontré une erreur</param>
        void iTcpClient.OnError(Exception exception, TcpClient client)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.LogEcrire("Erreur de [" + client.ServerIP + "]: " + exception.Message);
            }));
        }
        #endregion
    }
}
