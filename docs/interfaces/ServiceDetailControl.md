Nom: ServiceDetailControl
Fichier: SEEK_MANAGER\ServiceDetailControl.cs
Description:
- Contrôle affichant les détails d'un service sélectionné (description, tarifs, personnel).
- Utilisé conjointement avec `ServiceControl` pour montrer les informations détaillées.

Usage:
- Charger un service via `LoadService(int id)` ou propriété `ServiceId`.
- Afficher/éditer les détails et sauvegarder via le dépôt.

Dépendances:
- `SERVICES.cs`, `ServiceControl.cs`, `Repository.cs`.

Remarques:
- Vérifier l'existence des méthodes publiques `Load`, `Save`.
