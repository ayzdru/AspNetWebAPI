// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace Example.IdentityServer4WithAspNetCoreIdentity
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> Ids =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
            };


        public static IEnumerable<ApiResource> Apis =>
            new List<ApiResource>
            {
                new ApiResource
            {
                Name = "api1",               
                DisplayName = "Example.APIWithSwagger",
                Scopes = new[]
                {                        
                   
                    new Scope("readAccess", "Access read operations"),
                    new Scope("writeAccess", "Access write operations")
                }
            }
            };

        public static IEnumerable<Client> Clients =>
            new List<Client>
            {
                new Client
            {
                ClientId = "test-id",
                ClientName = "Test client (Code with PKCE)",

                RedirectUris = new[] {
                    "http://localhost:5001/swagger/oauth2-redirect.html", // Kestrel
                },

                ClientSecrets = { new Secret("test-secret".Sha256()) },
                RequireConsent = false,

                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                AllowedScopes = new[] { "readAccess", "writeAccess" },
            },
                // machine to machine client
                new Client
                {
                    ClientId = "client",
                    ClientSecrets = { new Secret("secret".Sha256()) },

                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    // scopes that client has access to
                    AllowedScopes = new[] { "readAccess", "writeAccess" }
                },
                // interactive ASP.NET Core MVC client
                new Client
                {
                    ClientId = "mvc",
                    ClientSecrets = { new Secret("secret".Sha256()) },

                    AllowedGrantTypes = GrantTypes.Code,
                    RequireConsent = false,
                    RequirePkce = true,
                
                    // where to redirect to after login
                    RedirectUris = { "http://localhost:5002/signin-oidc" },

                    // where to redirect to after logout
                    PostLogoutRedirectUris = { "http://localhost:5002/signout-callback-oidc" },

                      AllowedScopes = new[] { IdentityServerConstants.StandardScopes.OpenId, IdentityServerConstants.StandardScopes.Profile, "readAccess", "writeAccess" },

                    AllowOfflineAccess = true
                },
                // JavaScript Client
                new Client
                {
                    ClientId = "js",
                    ClientName = "JavaScript Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RequireClientSecret = false,

                    RedirectUris =           { "http://localhost:5003/callback" },
                    PostLogoutRedirectUris = { "http://localhost:5003/" },
                    AllowedCorsOrigins =     { "http://localhost:5003" },

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "readAccess", "writeAccess"
                    }
                },
                 new Client
        {
            ClientId = "ro.client",
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

            ClientSecrets =
            {
                new Secret("secret".Sha256())
            },
            AllowedScopes = { 
                        "readAccess", "writeAccess" },
             AllowOfflineAccess = true
        }
            };
    }
}