using FinanceData.Models;
using FinanceData.Services;

namespace FinanceData.Tests
{
    [TestClass]
    public class DatabaseServiceTests
    {
        private DatabaseService _service;
        private string _testDbPath;

        // CETTE MÉTHODE EST EXÉCUTÉE AUTOMATIQUEMENT AVANT CHAQUE TEST
        [TestInitialize]
        public void TestInitialize()
        {
            // 1. On crée un nom de fichier unique pour la base de données de ce test
            _testDbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db3");

            // 2. On crée une nouvelle instance du service pour CE test, avec SA base de données
            _service = new DatabaseService(_testDbPath);
        }

        // CETTE MÉTHODE EST EXÉCUTÉE AUTOMATIQUEMENT APRÈS CHAQUE TEST
        [TestCleanup]
        public async Task TestCleanup()
        {
            // 2. ON APPELLE LE NETTOYAGE DU SERVICE D'ABORD
            //    Cela va fermer la connexion et libérer le verrou sur le fichier.
            await _service.DisposeAsync();

            // 3. MAINTENANT, ON PEUT SUPPRIMER LE FICHIER SANS ERREUR
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }

        [TestMethod] // Attribut qui indique que cette méthode est un test
        public async Task AddAccountAsync_Should_SaveAccountToDatabase()
        {
            // 2. On prépare les données que l'on veut insérer.
            var newAccount = new Account { Name = "Compte Courant" };

            // ----- ACT -----
            // 3. On exécute l'action que l'on veut tester.
            await _service.AddAccountAsync(newAccount);


            // ----- ASSERT -----
            // 4. On vérifie que le résultat est celui attendu.
            var accounts = await _service.GetAccountsAsync();

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
            var account = new Account { Name = "Livret A" };
            await _service.AddAccountAsync(account);

            // On ajoute des entrées dans le désordre
            var entryToday = new BalanceEntry { AccountId = account.Id, Value = 1000, Date = DateTime.Today };
            var entryYesterday = new BalanceEntry { AccountId = account.Id, Value = 500, Date = DateTime.Today.AddDays(-1) };
            var entryLastWeek = new BalanceEntry { AccountId = account.Id, Value = 200, Date = DateTime.Today.AddDays(-7) };

            await _service.AddBalanceEntryAsync(entryToday);
            await _service.AddBalanceEntryAsync(entryLastWeek);
            await _service.AddBalanceEntryAsync(entryYesterday);

            // ACT
            var history = await _service.GetBalanceHistoryForAccountAsync(account.Id);

            // ASSERT
            // On vérifie qu'on a bien 3 entrées
            Assert.AreEqual(3, history.Count);

            // On vérifie que la première entrée de la liste est bien la plus ancienne
            Assert.AreEqual(entryLastWeek.Value, history[0].Value);
            Assert.AreEqual(entryYesterday.Value, history[1].Value);
            Assert.AreEqual(entryToday.Value, history[2].Value);
        }

        [TestMethod]
        public async Task GetMonthlyReportsAsync_Should_CalculateEvolutionCorrectly()
        {
            // ----- ARRANGE -----
            var account = new Account { Name = "Test Account" };
            await _service.AddAccountAsync(account);

            // On ajoute des données sur plusieurs mois.
            // y a 2 transactions en juillet. Le service doit prendre la plus récente (150).
            await _service.AddBalanceEntryAsync(new BalanceEntry { AccountId = account.Id, Value = 100, Date = new DateTime(2025, 7, 10) });
            await _service.AddBalanceEntryAsync(new BalanceEntry { AccountId = account.Id, Value = 150, Date = new DateTime(2025, 7, 25) });

            // Une seule transaction en août.
            await _service.AddBalanceEntryAsync(new BalanceEntry { AccountId = account.Id, Value = 350, Date = new DateTime(2025, 8, 5) });

            // Pas de transaction en septembre (test d'un "trou" dans les données).

            // Une transaction en octobre.
            await _service.AddBalanceEntryAsync(new BalanceEntry { AccountId = account.Id, Value = 300, Date = new DateTime(2025, 10, 15) });


            // ----- ACT -----
            var reports = await _service.GetMonthlyReportsAsync(account.Id);


            // ----- ASSERT -----
            // On doit avoir 3 rapports, un pour chaque mois avec des données.
            Assert.AreEqual(3, reports.Count);

            // Rapport de Juillet 2025
            var julyReport = reports[0];
            Assert.AreEqual(150, julyReport.LastEntryOfMonth.Value); // Doit prendre la dernière valeur de juillet
            Assert.AreEqual(0, julyReport.EvolutionValue); // Pas de mois précédent, donc évolution nulle

            // Rapport d'Août 2025
            var augustReport = reports[1];
            Assert.AreEqual(350, augustReport.LastEntryOfMonth.Value);
            Assert.AreEqual(200, augustReport.EvolutionValue); // 350 - 150
            Assert.AreEqual(200m / 150m, augustReport.EvolutionPercentage); // (350 - 150) / 150

            // Rapport d'Octobre 2025
            var octoberReport = reports[2];
            Assert.AreEqual(300, octoberReport.LastEntryOfMonth.Value);
            Assert.AreEqual(-50, octoberReport.EvolutionValue); // 300 - 350
            Assert.AreEqual(-50m / 350m, octoberReport.EvolutionPercentage); // (300 - 350) / 350
        }

        [TestMethod]
        public async Task UpdateAccountAsync_Should_ChangeAccountName()
        {
            // ARRANGE
            var originalAccount = new Account { Name = "Ancien Nom" };
            await _service.AddAccountAsync(originalAccount);

            // ACT
            originalAccount.Name = "Nouveau Nom";
            await _service.UpdateAccountAsync(originalAccount);

            // ASSERT
            var updatedAccount = (await _service.GetAccountsAsync()).First();
            Assert.AreEqual("Nouveau Nom", updatedAccount.Name);
        }

        [TestMethod]
        public async Task DeleteAccountAsync_Should_RemoveAccountAndItsEntries()
        {
            // ARRANGE
            var account = new Account { Name = "Compte à supprimer" };
            await _service.AddAccountAsync(account);
            await _service.AddBalanceEntryAsync(new BalanceEntry { AccountId = account.Id, Value = 100 });
            await _service.AddBalanceEntryAsync(new BalanceEntry { AccountId = account.Id, Value = 200 });

            // ACT
            await _service.DeleteAccountAsync(account.Id);

            // ASSERT
            var accounts = await _service.GetAccountsAsync();
            var entries = await _service.GetBalanceHistoryForAccountAsync(account.Id);
            Assert.AreEqual(0, accounts.Count, "Le compte n'a pas été supprimé.");
            Assert.AreEqual(0, entries.Count, "Les entrées du compte n'ont pas été supprimées.");
        }

        [TestMethod]
        public async Task UpdateBalanceEntryAsync_Should_ChangeEntryValue()
        {
            // ARRANGE
            var account = new Account { Name = "Compte test" };
            await _service.AddAccountAsync(account);
            var originalEntry = new BalanceEntry { AccountId = account.Id, Value = 1234 };
            await _service.AddBalanceEntryAsync(originalEntry);

            // ACT
            originalEntry.Value = 5678;
            await _service.UpdateBalanceEntryAsync(originalEntry);

            // ASSERT
            var updatedEntry = (await _service.GetBalanceHistoryForAccountAsync(account.Id)).First();
            Assert.AreEqual(5678, updatedEntry.Value);
        }

        [TestMethod]
        public async Task DeleteBalanceEntryAsync_Should_RemoveOnlyOneEntry()
        {
            // ARRANGE
            var account = new Account { Name = "Compte test" };
            await _service.AddAccountAsync(account);
            var entryToDelete = new BalanceEntry { AccountId = account.Id, Value = 100 };
            var entryToKeep = new BalanceEntry { AccountId = account.Id, Value = 200 };
            await _service.AddBalanceEntryAsync(entryToDelete);
            await _service.AddBalanceEntryAsync(entryToKeep);

            // ACT
            await _service.DeleteBalanceEntryAsync(entryToDelete.Id);

            // ASSERT
            var entries = await _service.GetBalanceHistoryForAccountAsync(account.Id);
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(entryToKeep.Value, entries.First().Value);
        }
    }
}
