using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using Classes.TCP;

namespace TCPServerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, iTCPServerDelegate
    {
        #region Propriétés privées
        /// <summary>
        /// Objet serveur.
        /// </summary>
        TcpServer _serveur = null;
        /// <summary>
        /// Dictionnaire contenant les clients connectés (clé = "IP:port").
        /// </summary>
        Dictionary<string, TCPServerClient> _dictClients = new Dictionary<string, TCPServerClient>();
        #endregion

        #region Constructeur
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region Evénements interface utilisateur
        private void BttnConnecter_Click(object sender, RoutedEventArgs e)
        {
            // Lancement de l'écoute TCP:
            int port = Convert.ToInt32(this.TxtPort.Text);

            if (port > 0 && port < 655535)
            {
                _serveur = new TcpServer(port, this);
                _serveur.ReferenceDelegate = this;
                _serveur.Start();

                EnableButtons(true);
            }
        }

        private void BttnDeconnecter_Click(object sender, RoutedEventArgs e)
        {
            // Arrêt de l'écoute TCP:
            if (_serveur != null)
            {
                _serveur.Stop();
                _serveur = null;
            }

            _dictClients = new Dictionary<string, TCPServerClient>();
            this.LstClients.ItemsSource = new List<string>();
            this.EnableButtons(false);
        }

        private void BttnSend_Click(object sender, RoutedEventArgs e)
        {
            // Envoi du texte de TxtMessage au client sélectionné dans la liste:
            if (_serveur != null && this.LstClients.SelectedIndex > -1)
            {
                // Obtention du client:
                string cle = (string)this.LstClients.SelectedItem;
                if (_dictClients.ContainsKey(cle) == true)
                {
                    TCPServerClient client = _dictClients[cle];

                    // Envoi des octets vers le client sélectionné:
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(this.TxtMessage.Text);
                    _serveur.Send(data, client);
                }
            }
        }

        private void LstClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Activation ou désactivation de la grille contenant le message et le bouton d'envoi:
            this.GrdMessage.IsEnabled = (this.LstClients.SelectedItems.Count > 0);
        }
        #endregion

        #region Méthodes interface iTCPServerDelegate
        /// <summary>
        /// Evénement déclenché lorsque la connection d'un client est acceptée.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        void iTCPServerDelegate.OnConnect(TcpServer server, TCPServerClient client)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.LogEcrire("Client [" + client.IpAddress + "] connecté");
                if (_dictClients.ContainsKey(client.IpAddress) == true)
                {
                    _dictClients.Remove(client.IpAddress);
                }

                _dictClients.Add(client.IpAddress, client);

                List<string> lst = _dictClients.Keys.ToList<string>();
                this.LstClients.ItemsSource = lst;

                this.EnableButtons(true);

            }));
        }

        /// <summary>
        /// Evénement déclenché lorsqu'un client de la liste se déconnecte.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        void iTCPServerDelegate.OnDisconnect(TcpServer server, TCPServerClient client)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.LogEcrire("Client [" + client.IpAddress + "] déconnecté");

                if (_dictClients.ContainsKey(client.IpAddress) == true)
                {
                    _dictClients.Remove(client.IpAddress);
                }
                this.LstClients.ItemsSource = _dictClients.Values;

                this.EnableButtons(true);
            }));
            
        }

        /// <summary>
        /// Evénement déclenché lorsque des données en provenance d'un client sont reçues.
        /// </summary>
        /// <param name="buffer">Octets reçus</param>
        /// <param name="client">Client ayant envoyé les données</param>
        void iTCPServerDelegate.onTcpReading(byte[] buffer, TCPServerClient client)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                string text = Encoding.UTF8.GetString(buffer);
                LogEcrire("Message de [" + client.IpAddress + "]: " + text);
            }));
            
        }

        /// <summary>
        /// Evénement déclenché lorsque des données ont été envoyées avec succès vers un client.
        /// </summary>
        /// <param name="buffer">Octets envoyés</param>
        /// <param name="client">Client à qui les données ont été envoyées</param>
        void iTCPServerDelegate.onTcpSent(byte[] buffer, TCPServerClient client)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                string text = Encoding.UTF8.GetString(buffer);
                LogEcrire("["+text+"] => envoyé à [" + client.IpAddress + "]");
            }));
        }
        #endregion

        #region Méthodes privées
        /// <summary>
        /// Active ou non les boutons suivant l'état d'écoute.
        /// </summary>
        /// <param name="isConnected"></param>
        private void EnableButtons(bool isConnected)
        {
            this.BttnConnecter.IsEnabled = !isConnected;
            this.BttnDeconnecter.IsEnabled = isConnected;
            this.GrdMessage.IsEnabled = (this.LstClients.SelectedItems.Count > 0);
        }

        /// <summary>
        /// Ecrit une ligne de log dans la zone de texte du bas de la fenêtre.
        /// </summary>
        /// <param name="texte"></param>
        private void LogEcrire(string texte)
        {
            this.TxtLog.Text += texte + "\r\n";
        }        
        #endregion
 
    }
}
