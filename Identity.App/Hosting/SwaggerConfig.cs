using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using NSwag;
using NSwag.CodeGeneration.TypeScript;
using NSwag.Generation.Processors.Security;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.OperationNameGenerators;
using NSwag.Generation.Processors.Contexts;
using NSwag.Generation.Processors;
using NSwag.Generation.AspNetCore;

namespace Identity.App.Hosting;
public static class SwaggerConfig
{
    const string swaggerSpecUrl = "swagger/v1/swagger.json";
    //const string outputFolder = "{ProjectName}.Client/src/resources/api-clients/";

    public static IHostApplicationBuilder ConfigureSwagger(this IHostApplicationBuilder app)
    {
        var configuraration = app.Configuration;
        var services = app.Services;

        services.AddEndpointsApiExplorer();
        //services.AddSwaggerGen();
        services.AddOpenApiDocument(options =>
        {
            options.AddOperationFilter(context =>
            {

                if (context is AspNetCoreOperationProcessorContext asp)
                {
                    var apiDesc = asp.ApiDescription;

                    var endpointName = apiDesc.ActionDescriptor?.EndpointMetadata?
                        .OfType<IEndpointNameMetadata>()
                        .LastOrDefault()?.EndpointName;

                    endpointName ??= apiDesc.ActionDescriptor?.AttributeRouteInfo?.Name;


                    if (!string.IsNullOrEmpty(endpointName))
                    {
                        //context.Document.
                        context.OperationDescription.Operation.OperationId = endpointName;
                    }

                }
                else
                    return false;
                return true;
            });

            //options.Name

            //options.AddSecurity("token", new OpenApiSecurityScheme
            //{
            //    In = OpenApiSecurityApiKeyLocation.Header,
            //    Name = "Authorization",
            //    Type = OpenApiSecuritySchemeType.ApiKey
            //});

            //options.OperationProcessors.Add(
            //    new OperationSecurityScopeProcessor("token"));
        });

        return app;
    }

    public static IApplicationBuilder UseSwagger(this WebApplication app)
    {
        app.UseOpenApi(config =>
        {
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerUi();
            app.UseReDoc();
        }



        return app;
    }

    public async static Task GenerateClients(this WebApplication app)
    {
        await app.GenerateDotNetClient();
        await app.GenerateTypescriptClient();
    }


    public async static Task GenerateDotNetClient(this WebApplication app)
    {
        var options = app.Configuration.GetSection("ClientGeneration:DotNet")?.Get<ClientGenerationOptions>();
        if (options?.Enabled != true)
            return;

        var server = app.Services.GetService<IServer>();
        var addF = server?.Features.Get<IServerAddressesFeature>();
        var baseUrl = addF?.Addresses.FirstOrDefault();

        if (baseUrl == null)
        {
            throw new InvalidOperationException("Failed to get base url");
        }

        var uri = new Uri(new Uri(baseUrl), swaggerSpecUrl);
        var document = await OpenApiDocument.FromUrlAsync(uri.AbsoluteUri);


        var settings = new CSharpClientGeneratorSettings
        {
            ClassName = "{controller}Client",
            CSharpGeneratorSettings =
            {
              Namespace = "IdentityOidc.API"
            },
            ClientBaseClass = "ClientBase",
            GenerateClientClasses = true,
            GenerateClientInterfaces = false,
            GenerateOptionalParameters = true,
        };

        settings.OperationNameGenerator = new MultipleClientsFromFirstTagAndOperationNameGenerator();

        var outputFolder = Environment.CurrentDirectory;
        if (options.ClientPath != null)
            outputFolder = Path.Combine(outputFolder, options.ClientPath);

      
        var filePath = Path.Combine(outputFolder, $"{options.ClientName}.cs");

        var generator = new CSharpClientGenerator(document, settings);
        var code = generator.GenerateFile();

        Directory.CreateDirectory(outputFolder);
        File.WriteAllText(filePath, code);
    }

    public async static Task GenerateTypescriptClient(this WebApplication app)
    {
        var options = app.Configuration.GetSection("ClientGeneration:TypeScript")?.Get<ClientGenerationOptions>();
        if (options?.Enabled != true)
            return;


        var server = app.Services.GetService<IServer>();
        var addF = server?.Features.Get<IServerAddressesFeature>();
        var baseUrl = addF?.Addresses.FirstOrDefault();

        if (baseUrl == null)
        {
            throw new InvalidOperationException("Failed to get base url");
        }

        var uri = new Uri(new Uri(baseUrl), swaggerSpecUrl);
        var document = await OpenApiDocument.FromUrlAsync(uri.AbsoluteUri);

        var settings = new TypeScriptClientGeneratorSettings
        {
            ClassName = "{controller}Client",
            OperationNameGenerator = new MultipleClientsFromFirstTagAndPathSegmentsOperationNameGenerator(),
            Template = TypeScriptTemplate.Fetch,
            TypeScriptGeneratorSettings =
            {
                TypeScriptVersion = 5.0m,
                //ExtensionCode = @"",
            },
            HttpClass = HttpClass.HttpClient,
            BaseUrlTokenName = baseUrl,
            InjectionTokenType = InjectionTokenType.OpaqueToken,
            ClientBaseClass = "ClientBase",
            GenerateClientClasses = true,
            GenerateClientInterfaces = false,
            GenerateOptionalParameters = true,
            UseGetBaseUrlMethod = true,
            UseTransformOptionsMethod = true,
            UseTransformResultMethod = true,
        };

        settings.OperationNameGenerator = new MultipleClientsFromFirstTagAndOperationNameGenerator();

        var outputFolder = Environment.CurrentDirectory;
        if (options.ClientPath != null)
            outputFolder = Path.Combine(outputFolder, options.ClientPath);

        if (options.Extend == true)
        {
            var extensionFilePath = Path.Combine(outputFolder, $"{options.ClientName}-extension.ts");
            if (File.Exists(extensionFilePath))
                settings.TypeScriptGeneratorSettings.ExtensionCode = await File.ReadAllTextAsync(extensionFilePath);
            else
                settings.TypeScriptGeneratorSettings.ExtensionCode = $"//Extension Not Found at {extensionFilePath}";
        }
        var filePath = Path.Combine(outputFolder, $"{options.ClientName}.ts");

        var generator = new TypeScriptClientGenerator(document, settings);
        var code = generator.GenerateFile();

        Directory.CreateDirectory(outputFolder);
        File.WriteAllText(filePath, code);
    }

    private class ClientGenerationOptions
    {
        public bool Enabled { get; set; } = false;
        public string? ClientPath { get; set; }

        public string? ClientName { get; set; } = "client";
        public bool? Extend { get; set; } = false;

    }

    //class MinimalNameGenerator : IOperationNameGenerator
    //{
    //    public bool SupportsMultipleClients => false;

    //    public string GetClientName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
    //    {
    //        return operation.Tags?.FirstOrDefault() ?? "DefaultClient";
    //    }

    //    public string GetOperationName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
    //    {
    //        return operation.OperationId;
    //    }
    //}

}
