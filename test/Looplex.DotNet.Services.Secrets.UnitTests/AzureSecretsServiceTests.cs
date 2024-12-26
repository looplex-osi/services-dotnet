using Looplex.DotNet.Core.Application.Abstractions.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Looplex.DotNet.Services.Secrets.UnitTests
{
    [TestClass]
    public class AzureSecretsServiceTests
    {
        private ISecretsService _secretsService = null!;        

        [TestInitialize]
        public void Setup()
        {
            _secretsService = Substitute.For<ISecretsService>();
        }

        [DataRow("looplex.com.br")]        
        [TestMethod]
        public async Task GetSecretAsync_ValueIsReturned_WhenSecretNameIsFound(string secretName)
        {
            string? secretValue = await _secretsService.GetSecretAsync(secretName);
            Assert.AreEqual("Hardcode", secretValue);
        }

        [DataRow("nomeinexistente.com.br")]
        [TestMethod]
        public async void GetSecretAsync_NullIsReturned_WhenSecretNameIsNotFound(string secretName)
        {
            string? secretKey = await _secretsService.GetSecretAsync(secretName);
            
            Assert.AreEqual(null, secretKey);
        }

        [DataRow(null)]
        [DataRow("")]
        [TestMethod]
        public void GetSecretAsync_ThrowException_WhenSecretNameIsNullOrEmpty(string secretName)
        {
            Assert.ThrowsException<Exception>(async () => { await _secretsService.GetSecretAsync(secretName); });                     
        }
    }
}
