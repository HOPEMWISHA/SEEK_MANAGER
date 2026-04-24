Nom: MainDashboard
Fichier: SEEK_MANAGER\MainDashboard.cs
Description:
- Form principal de l'application qui assemble les différents contrôles (patients, paiements, consultations, etc.).
- Fournit la navigation entre différentes vues et tableaux de bord.

Usage:
- Lancer en tant que `Application.Run(new MainDashboard())` depuis `Program.cs`.
- Utilise `UserSession` pour déterminer les droits et afficher/masquer les contrôles.

Dépendances:
- `UserSession.cs`, `PatientControl.cs`, `PaiementControl.cs`, `ServiceControl.cs`, `MedecinControl.cs`.

Remarques:
- Documenter les méthodes publiques pour manipuler la navigation et l'état du tableau de bord.
