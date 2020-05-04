﻿using System;
using System.Drawing;
using System.Threading.Tasks;

using AuthApp.Internal;

using McMaster.Extensions.CommandLineUtils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Console = Colorful.Console;

namespace AuthApp
{
    [Command(
        "get-tokens",
        Description = "Generates Salesforce Access and Refresh Tokens",
        UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.Throw,
        AllowArgumentSeparator = true)]
    [HelpOption("-?")]
    internal class TokenGeneratorCommand
    {
        [Option("-k|--key", Description = "The Salesforce Consumer Key.")]
        public string? ClientId { get; set; }

        [Option("-s|--secret", Description = "The Salesforce Consumer Secret.")]
        public string? ClientSecret { get; set; }

        [Option("-l|--login", Description = "The Salesforce login url. The default value is https://login.salesforce.com.")]
        public string? LoginUrl { get; set; }

        [Option(
            "-a|--azure",
            Description = "Allows to specify Azure Vault Url. It overrides url specified in the appsetting.json file or any other configuration provider.")]
        public string? AzureVault { get; set; }

        [Option("-ax|--azureprefix", Description = "Enables or disables Hosting Environment prefix to be used for Azure Key Vault. Default is true.")]
        public bool UseAzureKeyPrefix { get; set; }

        [Option("-c|--configfile", Description = "Allows to specify a configuration file besides appsettings.json to be specified.")]
        public string? ConfigFile { get; set; }

        /// <summary>
        /// Property types of ValueTuple{bool,T} translate to CommandOptionType.SingleOrNoValue.
        /// Input                   | Value
        /// ------------------------|--------------------------------
        /// (none)                  | (false, default(LogLevel))
        /// --verbose               | (true, LogLevel.Information)
        /// --verbose:information   | (true, LogLevel.Information)
        /// --verbose:debug         | (true, LogLevel.Debug)
        /// --verbose:trace         | (true, LogLevel.Trace).
        /// </summary>
        [Option(Description = "Allows Verbose logging for the tool. Enable this to get tracing information. Default is false and LogLevel.None.")]
        public (bool HasValue, LogLevel level) Verbose { get; } = (false, LogLevel.None);

        [Option("-us|--usesecrets", Description = "Enable UserSecrets.")]
        public bool UserSecrets { get; set; }

        [Option("-e|--environment", Description = "Specify Hosting Environment Name for the cli tool execution.")]
        public string? HostingEnviroment { get; set; }

        [Option("-sn|--section", Description = "Configuration Section Name to retrieve the options. The Default value is Salesforce.")]
        public string? SectionName { get; set; }

        private async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var builderConfig = new HostBuilderOptions
            {
                AzureVault = AzureVault,
                UseAzureKeyPrefix = !UseAzureKeyPrefix,
                ConfigFile = ConfigFile,
                Verbose = Verbose.HasValue,
                Level = Verbose.level,
                UserSecrets = UserSecrets,
                HostingEnviroment = !string.IsNullOrWhiteSpace(HostingEnviroment) ? HostingEnviroment ?? "Development" : "Development",
                Settings = new SfConfig
                {
                    ClientId = ClientId,
                    ClientSecret = ClientSecret,
                    LoginUrl = !string.IsNullOrWhiteSpace(LoginUrl) ? LoginUrl : "https://login.salesforce.com"
                },
                SectionName = string.IsNullOrWhiteSpace(SectionName) ? "Salesforce" : SectionName ?? "Salesforce",
            };

            try
            {
                var builder = HostBuilderExtensions.CreateDefaultBuilder(builderConfig)
                                .ConfigureServices((hostingContext, services) =>
                                {
                                    services.ConfigureWithDataAnnotationsValidation<SfConfig>(hostingContext.Configuration, builderConfig.SectionName);
                                    services.AddHostedService<HttpServer>();
                                });

                await builder.RunConsoleAsync();

                return 0;
            }
            catch (OptionsValidationException exv)
            {
                Console.WriteLine($"Not all of the required configurations has been provided. {exv.Message}", Color.Red);

                app.ShowHelp();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, Color.Red);
            }

            return 0;
        }
    }
}
