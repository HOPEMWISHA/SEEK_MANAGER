Nom: PatientControl
Fichier: SEEK_MANAGER\PatientControl.cs
Description:
- Contrôle d'interface pour l'affichage et la gestion des patients (liste, sélection, recherche).
- Présente les patients via l'entité `PATIENT` et le dépôt `Repository` / `MySqlDbManager`.

Usage:
- Instancier `PatientControl` et l'ajouter à un `Form` ou à un conteneur (panel).
- Appeler les méthodes publiques (rafraîchir/charger) si elles existent pour mettre à jour la liste.

Dépendances:
- `PATIENT.cs`, `PATIENT.Designer.cs`
- `Repository.cs`, `MySqlDbManager.cs`

Fichiers liés:
- SEEK_MANAGER\PatientControl.resx

Remarques:
- Vérifier les événements exposés (ex: `OnPatientSelected`, `OnRefreshRequested`) dans le code pour gérer les interactions.
- TODO: documenter les méthodes publiques précises si vous souhaitez des instructions d'intégration détaillées.
