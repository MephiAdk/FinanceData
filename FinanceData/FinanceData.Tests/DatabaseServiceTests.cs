using FinanceData.Models;
using FinanceData.Services;

namespace FinanceData.Tests
{
    [TestClass]
    public class DatabaseServiceTests
    {
        private readonly string _inMemoryDbPath = ":memory:";

        [TestMethod] // Attribut qui indique que cette méthode est un test
        public async Task AddAccountAsync_Should_SaveAccountToDatabase()
        {
            // ----- ARRANGE -----
            // 1. On crée une instance de notre service avec la base de données en mémoire.
            var service = new DatabaseService(_inMemoryDbPath);

            // 2. On prépare les données que l'on veut insérer.
            var newAccount = new Account { Name = "Compte Courant" };

            // ----- ACT -----
            // 3. On exécute l'action que l'on veut tester.
            await service.AddAccountAsync(newAccount);


            // ----- ASSERT -----
            // 4. On vérifie que le résultat est celui attendu.
            var accounts = await service.GetAccountsAsync();

            // On vérifie qu'il y a bien un seul compte dans la liste.
            Assert.AreEqual(1, accounts.Count);

            // On récupère ce compte.
            var retrievedAccount = accounts.First();

            // On vérifie que son nom est correct.
            Assert.AreEqual("Compte Courant", retrievedAccount.Name);
        }

        [TestMethod]
        public async Task GetBalanceHistoryForAccountAsync_Should_ReturnOrderedByDate()
        {
            // ARRANGE
            var service = new DatabaseService(_inMemoryDbPath);
            var account = new Account { Name = "Livret A" };
            await service.AddAccountAsync(account);

            // On ajoute des entrées dans le désordre
            var entryToday = new BalanceEntry { AccountId = account.Id, Value = 1000, Date = DateTime.Today };
            var entryYesterday = new BalanceEntry { AccountId = account.Id, Value = 500, Date = DateTime.Today.AddDays(-1) };
            var entryLastWeek = new BalanceEntry { AccountId = account.Id, Value = 200, Date = DateTime.Today.AddDays(-7) };

            await service.AddBalanceEntryAsync(entryToday);
            await service.AddBalanceEntryAsync(entryLastWeek);
            await service.AddBalanceEntryAsync(entryYesterday);

            // ACT
            var history = await service.GetBalanceHistoryForAccountAsync(account.Id);

            // ASSERT
            // On vérifie qu'on a bien 3 entrées
            Assert.AreEqual(3, history.Count);

            // On vérifie que la première entrée de la liste est bien la plus ancienne
            Assert.AreEqual(entryLastWeek.Value, history[0].Value);
            Assert.AreEqual(entryYesterday.Value, history[1].Value);
            Assert.AreEqual(entryToday.Value, history[2].Value);
        }
    }
}
