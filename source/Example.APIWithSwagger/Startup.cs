namespace Example.APIWithSwagger
{
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Builder;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc.ApiExplorer;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.PlatformAbstractions;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using static Microsoft.AspNet.OData.Query.AllowedQueryOptions;
    using static Microsoft.AspNetCore.Mvc.CompatibilityVersion;
    using static Microsoft.OData.ODataUrlKeyDelimiter;
   
    /// <summary>
    /// Represents the startup process for the application.
    /// </summary>
    public class Startup
    {
        public const string AllowOriginPolicyName = "AllowOrigin";
        /// <summary>
        /// Configures services for the application.
        /// </summary>
        /// <param name="services">The collection of services to configure the application with.</param>
        public void ConfigureServices( IServiceCollection services )
        {
            services.AddCors(options =>
            {
                options.AddPolicy(AllowOriginPolicyName,
                builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .AddIdentityServerAuthentication(c =>
                {
                    c.Authority = "http://localhost:5000/";
                    c.RequireHttpsMetadata = false;
                    c.ApiName = "api1";
                });

            services.AddAuthorization(c =>
            {
                c.AddPolicy("readAccess", p => p.RequireClaim("scope", "readAccess"));
                c.AddPolicy("writeAccess", p => p.RequireClaim("scope", "writeAccess"));
            });


            // the sample application always uses the latest version, but you may want an explicit version such as Version_2_2
            // note: Endpoint Routing is enabled by default; however, it is unsupported by OData and MUST be false
            services.AddMvc( options => options.EnableEndpointRouting = false ).SetCompatibilityVersion( Latest );
            services.AddApiVersioning( options => options.ReportApiVersions = true );
            services.AddOData().EnableApiVersioning();
            services.AddODataApiExplorer(
                options =>
                {
                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                    options.GroupNameFormat = "'v'VVV";

                    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                    // can also be used to control the format of the API version in route templates
                    options.SubstituteApiVersionInUrl = true;

                    // configure query options (which cannot otherwise be configured by OData conventions)
                    options.QueryOptions.Controller<V2.PeopleController>()
                                        .Action( c => c.Get( default ) )
                                            .Allow( Skip | Count )
                                            .AllowTop( 100 )
                                            .AllowOrderBy( "firstName", "lastName" );

                    options.QueryOptions.Controller<V3.PeopleController>()
                                        .Action( c => c.Get( default ) )
                                            .Allow( Skip | Count )
                                            .AllowTop( 100 )
                                            .AllowOrderBy( "firstName", "lastName" );

                } );
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen(
                options =>
                {
                    // add a custom operation filter which sets default values
                    options.OperationFilter<SwaggerDefaultValues>();

                    // integrate xml comments
                    options.IncludeXmlComments( XmlCommentsFilePath );
                    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows
                        {
                            Password = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri("http://localhost:5000/connect/authorize"),
                                TokenUrl = new Uri("http://localhost:5000/connect/token"),
                                Scopes = new Dictionary<string, string>
                                {
                                    { "readAccess", "Access read operations" },
                                    { "writeAccess", "Access write operations" }
                                }

                            }
                        }
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                            },
                            new[] { "readAccess", "writeAccess" }
                        }
                    });



                    //    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                    //    {
                    //        Type = SecuritySchemeType.OAuth2,
                    //        Flows = new OpenApiOAuthFlows
                    //        {
                    //            AuthorizationCode = new OpenApiOAuthFlow
                    //            {
                    //                AuthorizationUrl = new Uri("http://localhost:5000/connect/authorize"),
                    //                TokenUrl = new Uri("http://localhost:5000/connect/token"),
                    //                Scopes = new Dictionary<string, string>
                    //            {
                    //                { "readAccess", "Access read operations" },
                    //                { "writeAccess", "Access write operations" }
                    //            }
                    //            }
                    //        }
                    //    });

                    //    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    //{
                    //    {
                    //        new OpenApiSecurityScheme
                    //        {
                    //            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                    //        },
                    //        new[] { "readAccess", "writeAccess" }
                    //    }
                    //});
                } );
        }

        /// <summary>
        /// Configures the application using the provided builder, hosting environment, and logging factory.
        /// </summary>
        /// <param name="app">The current application builder.</param>
        /// <param name="modelBuilder">The <see cref="VersionedODataModelBuilder">model builder</see> used to create OData entity data models (EDMs).</param>
        /// <param name="provider">The API version descriptor provider used to enumerate defined API versions.</param>
        public void Configure( IApplicationBuilder app, VersionedODataModelBuilder modelBuilder, IApiVersionDescriptionProvider provider )
        {
            app.UseCors(AllowOriginPolicyName);
            app.UseMvc(
                routeBuilder =>
                {
                    // the following will not work as expected
                    // BUG: https://github.com/OData/WebApi/issues/1837
                    // routeBuilder.SetDefaultODataOptions( new ODataOptions() { UrlKeyDelimiter = Parentheses } );
                    routeBuilder.ServiceProvider.GetRequiredService<ODataOptions>().UrlKeyDelimiter = Parentheses;

                    // global odata query options
                    routeBuilder.Count().Filter().OrderBy().Expand().Select().MaxTop(null);

                    routeBuilder.MapVersionedODataRoutes( "odata", "api", modelBuilder.GetEdmModels() );
                } );
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
            app.UseSwagger();
            app.UseSwaggerUI(
                options =>
                {
                    options.EnableDeepLinking();
                    options.OAuthClientId("ro.client");
                    options.OAuthClientSecret("secret");
                    options.OAuthAppName("test-app");
                    options.OAuthScopeSeparator(" ");
                    options.OAuthUsePkce();

                    //options.OAuthClientId("test-id");
                    //options.OAuthClientSecret("test-secret");
                    //options.OAuthAppName("test-app");
                    //options.OAuthScopeSeparator(" ");
                    //options.OAuthUsePkce();
                    // build a swagger endpoint for each discovered API version
                    foreach ( var description in provider.ApiVersionDescriptions )
                    {
                        options.SwaggerEndpoint( $"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant() );
                    }
                } );
        }

        static string XmlCommentsFilePath
        {
            get
            {
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var fileName = typeof( Startup ).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return Path.Combine( basePath, fileName );
            }
        }
    }
}