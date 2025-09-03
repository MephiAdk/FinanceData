using FinanceData.Models;
using SQLite;

namespace FinanceData.Services
{
    public class DatabaseService
    {
        private readonly Lazy<Task<SQLiteAsyncConnection>> _databaseInitializer;

        public DatabaseService(string dbPath)
        {
            // Quand DatabaseService est créé, on ne fait RIEN d'intensif.
            // On prépare juste la logique d'initialisation pour plus tard.
            _databaseInitializer = new Lazy<Task<SQLiteAsyncConnection>>(() =>
            {
                // Cette fonction sera appelée automatiquement par .NET la première
                // fois (et seulement la première fois) que l'on accédera à _databaseInitializer.Value
                async Task<SQLiteAsyncConnection> InitializeDatabaseAsync()
                {
                    var database = new SQLiteAsyncConnection(dbPath);
                    await database.CreateTableAsync<Account>();
                    await database.CreateTableAsync<BalanceEntry>();
                    return database;
                }

                return InitializeDatabaseAsync();
            });
        }

        // Propriété privée pour accéder facilement à la connexion initialisée
        private Task<SQLiteAsyncConnection> Database => _databaseInitializer.Value;

        // --- Opérations sur les Comptes ---
        public async Task<List<Account>> GetAccountsAsync()
        {
            var db = await Database; // La 1ère fois, cela déclenche l'initialisation. Les fois suivantes, ça retourne l'instance existante.
            return await db.Table<Account>().ToListAsync();
        }

        public async Task<int> AddAccountAsync(Account account)
        {
            var db = await Database;
            return await db.InsertAsync(account);
        }

        // --- Opérations sur les Soldes ---
        public async Task<int> AddBalanceEntryAsync(BalanceEntry entry)
        {
            var db = await Database;
            return await db.InsertAsync(entry);
        }

        public async Task<List<BalanceEntry>> GetBalanceHistoryForAccountAsync(int accountId)
        {
            var db = await Database;
            return await db
                .Table<BalanceEntry>()
                .Where(entry => entry.AccountId == accountId)
                .OrderBy(entry => entry.Date)
                .ToListAsync();
        }
        public async Task<List<MonthlyReport>> GetMonthlyReportsAsync(int accountId)
        {
            var db = await Database;
            var allEntries = await GetBalanceHistoryForAccountAsync(accountId);

            // Si nous n'avons pas d'historique, nous retournons une liste vide.
            if (!allEntries.Any())
            {
                return new List<MonthlyReport>();
            }

            // 1. On regroupe toutes les transactions par année et par mois.
            // 2. Pour chaque groupe (chaque mois), on prend la transaction la plus récente.
            // 3. On s'assure que la liste de ces "dernières transactions" est triée par date.
            var lastEntryPerMonth = allEntries
                .GroupBy(entry => new { entry.Date.Year, entry.Date.Month })
                .Select(group => group.OrderByDescending(entry => entry.Date).First())
                .OrderBy(entry => entry.Date)
                .ToList();

            var reports = new List<MonthlyReport>();

            // On parcourt la liste des dernières transactions pour calculer l'évolution
            for (int i = 0; i < lastEntryPerMonth.Count; i++)
            {
                var currentMonthEntry = lastEntryPerMonth[i];

                // On crée le rapport de base avec la transaction du mois courant
                var report = new MonthlyReport(currentMonthEntry);

                // S'il existe un mois précédent dans notre liste...
                if (i > 0)
                {
                    var previousMonthEntry = lastEntryPerMonth[i - 1];

                    // On calcule l'évolution
                    report.EvolutionValue = currentMonthEntry.Value - previousMonthEntry.Value;

                    if (previousMonthEntry.Value != 0)
                    {
                        // On divise la différence par la valeur de départ pour obtenir le pourcentage
                        report.EvolutionPercentage = (report.EvolutionValue / previousMonthEntry.Value);
                    }
                }

                reports.Add(report);
            }

            // On retourne la liste des rapports complets.
            return reports;
        }
    }

}
