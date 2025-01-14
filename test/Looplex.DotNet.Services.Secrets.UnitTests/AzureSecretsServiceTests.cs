using Azure.Security.KeyVault.Secrets;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Looplex.DotNet.Services.Secrets.Exceptions;

namespace Looplex.DotNet.Services.Secrets.UnitTests
{
    [TestClass]
    public class AzureSecretsServiceTests ()
    {
        private AzureSecretsService _azureSecretService = null!;        
        private SecretClient _secretClient = null!; 
        private ILogger<AzureSecretsService> _logger = null!;
        
        [TestInitialize]
        public void Setup()
        {            
            _secretClient = Substitute.For<SecretClient>();
            _logger = Substitute.For<ILogger<AzureSecretsService>>();
            _azureSecretService = new AzureSecretsService(_secretClient, _logger);
        }

        [DataRow("looplex.com.br")]
        [TestMethod]
        public async Task GetSecretAsync_ValueIsReturned_WhenSecretNameIsFound(string secretName)
        {
            //Arrange
            string returnedValue = "returnedValue";
            var response = Substitute.For<Azure.Response<KeyVaultSecret>>();
            var keyVaultSecret = new KeyVaultSecret(secretName, returnedValue);

            response.Value.Returns(keyVaultSecret);
            _secretClient.GetSecretAsync(secretName).Returns(Task.FromResult(response));

            //Act
            var result = await _azureSecretService.GetSecretAsync(secretName);

            //Assert
            result.Should().Be(returnedValue);
        }
        
        [DataRow("")]
        [TestMethod]
        public void GetSecretAsync_ThrowException_WhenSecretNameIsEmpty(string secretName)
        {
            //Arrange
            _secretClient.GetSecretAsync(secretName);

            //Act
            var act = () => _azureSecretService.GetSecretAsync(secretName);

            //Assert
            var ex = Assert.ThrowsExceptionAsync<SecretValidationException>(act);
            Assert.AreEqual("The secret name is required.", ex.Result.Message);
        }

        [DataRow(null)]        
        [TestMethod]
        public void GetSecretAsync_ThrowException_WhenSecretNameIsNull(string secretName)
        {
            //Arrange
            _secretClient.GetSecretAsync(secretName);

            //Act
            var act = () => _azureSecretService.GetSecretAsync(secretName);

            //Assert
            var ex = Assert.ThrowsExceptionAsync<SecretValidationException>(act);
            Assert.AreEqual("The secret name is required.", ex.Result.Message);
        }

        [DataRow("nomeinexistente.com.br")]
        [TestMethod]
        public void GetSecretAsync_ExceptionReturned_WhenValueIsNull(string secretName)
        {
            //Arrange                        
            var response = Substitute.For<Azure.Response<KeyVaultSecret>>();
            response.Value.ReturnsNull();
            _secretClient.GetSecretAsync(secretName).Returns(Task.FromResult(response));

            //Act
            var act = () => _azureSecretService.GetSecretAsync(secretName);

            //Assert
            var ex = Assert.ThrowsExceptionAsync<KeyVaultException>(act);
            Assert.AreEqual("The secret value can not be null", ex.Result.Message);
        }

        [DataRow("nomeinexistente.com.br")]
        [TestMethod]
        public void GetSecretAsync_ExceptionReturned_WhenResponseIsNull(string secretName)
        {
            //Arrange
            var response = Substitute.For<Azure.Response<KeyVaultSecret>>();
            response = null;
            _secretClient.GetSecretAsync(secretName).Returns(Task.FromResult(response));

            //Act
            var act = () => _azureSecretService.GetSecretAsync(secretName);

            //Assert
            var ex = Assert.ThrowsExceptionAsync<KeyVaultException>(act);
            Assert.AreEqual("The response from Azure Key Vault can not be null", ex.Result.Message);
        }        
    }
}
