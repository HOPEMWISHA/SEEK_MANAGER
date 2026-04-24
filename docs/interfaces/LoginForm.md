Nom: LoginForm
Fichier: SEEK_MANAGER\LoginForm.cs
Description:
- Formulaire d'authentification de l'application. Gère la saisie du nom d'utilisateur et du mot de passe.
- Utilise `MySqlDbManager` ou `Repository` pour valider les identifiants.

Usage:
- `Application.Run(new LoginForm())` pour démarrer l'application en mode authentification.
- Après validation, créer `UserSession` et ouvrir `MainDashboard`.

Dépendances:
- `MySqlDbManager.cs`, `UserSession.cs`, `Repository.cs`.

Remarques:
- Vérifier les événements `OnLoginSuccess` / `OnLoginFailed` s'ils existent.
