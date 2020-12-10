# TCPServerClient
 Exemple de programme C#


Cette solution présente deux projets C# de type WPF : l'un est une application faisant office de serveur TCP, l'autre de client.

On peut lancer les deux applications en même temps depuis Visual Studio en lançant le débogage du projet sélectionné (nom en gras) avec le bouton "play" vert, puis en faisant clic droit / "Debug" / "Démarrer une nouvelle instance" sur l'autre projet dans l'explorateur de solution.

A noter qu'on peut également lancer plusieurs instances du programme client : elle pourront toutes se connecter au serveur et lui envoyer des messages (ils seront différenciés par l'adresse IP du client).

Cette application a pour but la démonstration de l'implémentation d'une classe Tcp Serveur et d'une classe Tcp Client. Le code peut être copié et reproduit sans autorisation.


# TcpServerApp

Permet de lancer l'écoute sur le poste actuel. Choisissez le port d'écoute (par défaut: 8200), puis cliquez sur le bouton "Ecouter" : le programme se met en attente de connexion de client(s). Attention : si un message Windows demande l'autorisation des connexions, il faudra l'accepter sans quoi l'application ne fonctionnera pas.

Une fois un client connecté, il est possible de lui envoyer un message en le sélectionnant dans la liste de gauche, puis en remplissant la zone de texte et en appuyant sur le bouton d'envoi de message : ce dernier doit apparaitre dans le journal de l'application cliente.


#TcpClientApp

Il s'agit de l'application client. Renseignez l'adresse IP du serveur et le port (ce doit être le même que celui renseigné dans l'application TcpServerApp) => le statut de connexion doit apparaître dans le journal en bas de la fenêtre.

Une fois la connexion établie, renseignez un message dans la zone de texte puis cliquez sur le bouton "Envoyer" : l'application serveur doit recevoir le message dans son journal.

