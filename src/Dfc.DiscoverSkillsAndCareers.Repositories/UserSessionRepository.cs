﻿using System;
using System.Net;
using System.Threading.Tasks;
using Dfc.DiscoverSkillsAndCareers.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Dfc.DiscoverSkillsAndCareers.Repositories
{
    public class UserSessionRepository
    {
        readonly ICosmosSettings cosmosSettings;
        readonly string collectionName;
        readonly DocumentClient client;

        public UserSessionRepository(ICosmosSettings cosmosSettings, string collectionName = "UserSessions")
        {
            this.cosmosSettings = cosmosSettings;
            this.collectionName = collectionName;
            client = new DocumentClient(new Uri(cosmosSettings.Endpoint), cosmosSettings.Key);
        }

        public async Task<UserSession> GetUserSession(string primaryKey)
        {
            int pos = primaryKey.IndexOf('-');
            string partitionKey = primaryKey.Substring(0, pos);
            string userSessionId = primaryKey.Substring(pos + 1, primaryKey.Length - (pos + 1));
            return await GetUserSessionAsync(userSessionId, partitionKey);
        }

        public async Task<UserSession> GetUserSessionAsync(string userSessionId, string partitionKey)
        {
            try
            {
                var uri = UriFactory.CreateDocumentUri(cosmosSettings.DatabaseName, collectionName, userSessionId);
                var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(partitionKey) };
                Document document = await client.ReadDocumentAsync(uri, requestOptions);
                return (UserSession)(dynamic)document;
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<Document> CreateUserSession(UserSession userSession)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(cosmosSettings.DatabaseName, collectionName);
            return await client.CreateDocumentAsync(uri, userSession);
        }

        public async Task<UserSession> UpdateUserSession(UserSession userSession)
        {
            var uri = UriFactory.CreateDocumentUri(cosmosSettings.DatabaseName, collectionName, userSession.UserSessionId);
            var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(userSession.PartitionKey) };
            var response = await client.ReplaceDocumentAsync(uri, userSession, requestOptions);
            var updated = response.Resource;
            return (UserSession)(dynamic)updated;
        }
    }
}