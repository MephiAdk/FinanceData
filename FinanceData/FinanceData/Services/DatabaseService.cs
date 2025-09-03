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
    }
}
