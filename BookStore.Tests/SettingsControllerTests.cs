using BookStore.API.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace BookStore.Tests
{
    public class SettingsControllerTests
    {
        [Fact]
        public async Task Save_And_Get_ShouldUseAppDataFolderUnderContentRootPath()
        {
            // Arrange
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var mockEnv = new Mock<IWebHostEnvironment>();
                mockEnv.Setup(e => e.ContentRootPath).Returns(tempDirectory);
                mockEnv.Setup(e => e.WebRootPath).Returns(Path.Combine(tempDirectory, "wwwroot"));

                var controller = new SettingsController(mockEnv.Object);
                var newSettings = new AdminAppSettings
                {
                    StoreName = "Secure BookStore",
                    StorePhone = "0900000000",
                    BankName = "Test Bank",
                    BankAccountNumber = "123456789"
                };

                // Act
                var saveResult = await controller.Save(newSettings);
                var getResult = await controller.Get() as JsonResult;

                // Assert
                var expectedPath = Path.Combine(tempDirectory, "App_Data", "admin_appsettings.json");
                Assert.True(File.Exists(expectedPath), $"Expected config file to exist at {expectedPath}");

                // Ensure file is NOT in wwwroot
                var publicPath = Path.Combine(tempDirectory, "wwwroot", "admin", "appsettings.json");
                Assert.False(File.Exists(publicPath), $"Config file should NOT be exposed in wwwroot at {publicPath}");

                Assert.NotNull(getResult);
                var loadedSettings = getResult!.Value as AdminAppSettings;
                Assert.NotNull(loadedSettings);
                Assert.Equal("Secure BookStore", loadedSettings!.StoreName);
                Assert.Equal("123456789", loadedSettings.BankAccountNumber);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }
    }
}
