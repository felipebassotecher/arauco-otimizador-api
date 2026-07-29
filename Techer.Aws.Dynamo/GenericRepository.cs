using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Techer.Common.Extensions;
using Techer.Common.Json;
using Techer.Common.Json.Converters;

namespace Techer.Aws.Dynamo
{
    public abstract class GenericRepository<T>
    {
        protected AmazonDynamoDBClient ddbClient;
        protected DynamoDBContext ddbContext;
        protected Table table;

        protected AsyncRetryPolicy retryPolicy;

        public GenericRepository(string awsRegion = "sa-east-1")
        {
            ddbClient = new AmazonDynamoDBClient(Amazon.RegionEndpoint.GetBySystemName(awsRegion));
            ddbContext = new DynamoDBContext(ddbClient);

            table = Table.LoadTable(ddbClient, typeof(T).GetAttributeValue((DynamoDBTableAttribute t) => t.TableName));

            var random = new Random();

            retryPolicy = Policy
                .Handle<ProvisionedThroughputExceededException>()
                .Or<AmazonDynamoDBException>(ex => ex.ErrorCode == "ThrottlingException")
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromSeconds(random.Next(100)));
        }

        protected async Task UpdateItemAsync(T value)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                await ddbContext.SaveAsync(value);
            });
        }

        protected async Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest updateItemRequest)
        {
            var res = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                return await ddbClient.UpdateItemAsync(updateItemRequest);
            });

            if (res.FinalException != null)
                throw res.FinalException;

            return res.Result;
        }

        protected async Task<T> UpdateItemWithConditionAsync(Document doc, Expression expr)
        {
            var res = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                return await table.UpdateItemAsync(doc, new UpdateItemOperationConfig
                {
                    ConditionalExpression = expr,
                    ReturnValues = ReturnValues.AllNewAttributes,
                });
            });

            if (res.Outcome == OutcomeType.Failure)
            {
                throw res.FinalException;
            }

            return ddbContext.FromDocument<T>(res.Result);
        }

        protected async Task<T> GetItemAsync(object hashKey, object sortKey = null, bool consistentRead = false)
        {
            var res = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                return await ddbContext.LoadAsync<T>(hashKey, sortKey, new DynamoDBOperationConfig
                {
                    ConsistentRead = consistentRead
                });
            });

            if (res.Outcome == OutcomeType.Failure)
                throw res.FinalException;

            return res.Result;
        }

        protected async Task<Document> GetDocumentAsync(Primitive hashKey, Primitive sortKey = null, bool consistentRead = false)
        {
            var res = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                return await table.GetItemAsync(hashKey, sortKey, new GetItemOperationConfig
                {
                    ConsistentRead = consistentRead
                });
            });

            if (res.Outcome == OutcomeType.Failure)
                throw res.FinalException;

            return res.Result;
        }

        protected async Task DeleteItemAsync(object hashKey, object sortKey = null)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                await ddbContext.DeleteAsync<T>(hashKey, sortKey);
            });
        }

        protected async Task<DeleteItemResponse> DeleteItemAsync(DeleteItemRequest deleteItemRequest)
        {
            var res = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                return await ddbClient.DeleteItemAsync(deleteItemRequest);
            });

            return res.Result;
        }

        protected async Task DeleteItemAsync(T value)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                await ddbContext.DeleteAsync(value);
            });
        }

        protected async Task BatchAddAsync(IEnumerable<T> items)
        {
            foreach (var grupo in items.Batch(25))
            {
                var batch = ddbContext.CreateBatchWrite<T>(new DynamoDBOperationConfig()
                {
                    SkipVersionCheck = true
                });

                batch.AddPutItems(grupo);

                await retryPolicy.ExecuteAsync(async () => await batch.ExecuteAsync());
            }
        }

        protected async Task BatchDeleteKeysAsync(IEnumerable<string> hashs)
        {
            foreach (var grupo in hashs.Batch(25))
            {
                var batch = ddbContext.CreateBatchWrite<T>();

                foreach (var item in grupo)
                    batch.AddDeleteKey(item);

                await retryPolicy.ExecuteAsync(async () => await batch.ExecuteAsync());
            }
        }

        protected async Task BatchDeleteAsync(IEnumerable<KeyValuePair<string, string>> keys)
        {
            foreach (var grupo in keys.Batch(25))
            {
                var batch = ddbContext.CreateBatchWrite<T>();

                foreach (var item in grupo)
                    batch.AddDeleteKey(item.Key, item.Value);

                await retryPolicy.ExecuteAsync(async () => await batch.ExecuteAsync());
            }
        }

        protected async Task BatchDeleteHashSortAsync<K, Y>(IEnumerable<KeyValuePair<K, Y>> keys)
        {
            foreach (var partition in keys.Batch(25))
            {
                var batch = ddbContext.CreateBatchWrite<T>();

                foreach (var item in partition)
                {
                    batch.AddDeleteKey(
                        new Primitive(item.Key.ToString(), item.Key is int || item.Key is long),
                        new Primitive(item.Value.ToString(), item.Value is int || item.Value is long));
                }

                await retryPolicy.ExecuteAsync(async () => await batch.ExecuteAsync());
            }
        }

        protected async Task<IEnumerable<Document>> BatchGetKeysAsync(IEnumerable<KeyValuePair<string, string>> keys, List<string> attributesToGet)
        {
            var res = new List<Document>();

            foreach (var grupo in keys.Batch(100))
            {
                var batch = table.CreateBatchGet();

                batch.AttributesToGet = attributesToGet;

                foreach (var item in grupo)
                    batch.AddKey(new Primitive(item.Key), new Primitive(item.Value));

                await retryPolicy.ExecuteAsync(async () => await batch.ExecuteAsync());

                res.AddRange(batch.Results);
            }

            return res;
        }

        protected async Task<IEnumerable<T>> BatchGetKeysAsync(IEnumerable<KeyValuePair<dynamic, dynamic>> keys)
        {
            var res = new List<Document>();

            foreach (var grupo in keys.Batch(100))
            {
                var batch = table.CreateBatchGet();

                foreach (var item in grupo)
                    batch.AddKey(item.Key, item.Value);
                //batch.AddKey(new Primitive(item.Key), new Primitive(item.Value));

                await retryPolicy.ExecuteAsync(async () => await batch.ExecuteAsync());

                res.AddRange(batch.Results);
            }

            return res
                .Select(d => ddbContext.FromDocument<T>(d))
                .ToList();
        }

        protected async Task<IEnumerable<Document>> BatchGetDocumentsKeysAsync(IEnumerable<KeyValuePair<dynamic, dynamic>> keys)
        {
            var res = new List<Document>();

            foreach (var grupo in keys.Batch(100))
            {
                var batch = table.CreateBatchGet();

                foreach (var item in grupo)
                    batch.AddKey(item.Key, item.Value);

                await retryPolicy.ExecuteAsync(async () => await batch.ExecuteAsync());

                res.AddRange(batch.Results);
            }

            return res;
        }

        protected async Task<IEnumerable<T>> BatchGetKeysAsync(IEnumerable<dynamic> keys)
        {
            var res = new List<Document>();

            foreach (var grupo in keys.Batch(100))
            {
                var batch = table.CreateBatchGet();

                foreach (var item in grupo)
                    batch.AddKey(new Primitive(item));

                await retryPolicy.ExecuteAsync(async () => await batch.ExecuteAsync());

                res.AddRange(batch.Results);
            }

            return res
                .Select(d => ddbContext.FromDocument<T>(d))
                .ToList();
        }

        protected async Task<DynamoDbDataSourceResult<T>> GetNextPaginatedAsync(QueryOperationConfig config, string[] chaves = null, bool revertWhenBackward = true)
        {
            var search = table.Query(config);

            var docs = (await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                return await search.GetNextSetAsync();
            })).Result;

            string tokenInicial = null;

            if (chaves != null && docs.Any())
            {
                var attrDic = new Dictionary<string, AttributeValue>();
                foreach (var chave in chaves)
                {
                    attrDic.Add(chave, docs.First().ToAttributeMap()[chave]);
                }

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.None,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    ContractResolver = new IgnoreEmptyEnumerableResolver()
                };

                tokenInicial = JsonHelper.Serialize(attrDic, settings);
            }

            var items = docs
                .Select(d => ddbContext.FromDocument<T>(d))
                .ToList();

            string afterToken = null;
            string beforeToken = null;

            if (config.BackwardSearch)
            {
                afterToken = tokenInicial;
                beforeToken = search.PaginationToken?.Length > 3 ? search.PaginationToken : null;

                if (revertWhenBackward)
                {
                    items.Reverse();
                }
            }
            else
            {
                afterToken = search.PaginationToken?.Length > 3 ? search.PaginationToken : null;
                beforeToken = tokenInicial;
            }

            return new DynamoDbDataSourceResult<T>
            {
                Cursor = new DynamoDbDataSourceCursor()
                {
                    AfterToken = afterToken,
                    BeforeToken = beforeToken
                },
                Data = items
            };
        }

        protected async Task<IEnumerable<T>> GetNextAsync(QueryOperationConfig config)
        {
            var search = table.Query(config);

            var docs = (await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                return await search.GetNextSetAsync();
            })).Result;

            var items = docs
                .Select(d => ddbContext.FromDocument<T>(d))
                .ToList();

            return items;
        }

        protected async Task<List<T>> GetAllAsync(QueryOperationConfig config)
        {
            Search search = table.Query(config);

            var docs = (await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                return await search.GetRemainingAsync();
            })).Result;

            return docs?
                .Select(d => ddbContext.FromDocument<T>(d))
                .ToList();
        }

        protected async Task<PutItemResponse> PutAsync(PutItemRequest request)
        {
            var res = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                return await ddbClient.PutItemAsync(request);
            });

            return res.Result;
        }

        protected async Task<DynamoDbDataSourceResult<T>> Scan(string token, ScanFilter filter, List<string> attributesToGet = null)
        {
            var search = table.Scan(new ScanOperationConfig
            {
                PaginationToken = token,
                Filter = filter,
                Select = attributesToGet == null ? SelectValues.AllAttributes : SelectValues.SpecificAttributes,
                AttributesToGet = attributesToGet
                //ConsistentRead = true
            });

            var docs = await search.GetNextSetAsync();

            var items = docs
                .Select(d => ddbContext.FromDocument<T>(d))
                .ToList();

            var afterToken = search.PaginationToken;

            if (!string.IsNullOrWhiteSpace(afterToken) && afterToken == "{}")
                afterToken = null;

            return new DynamoDbDataSourceResult<T>
            {
                Cursor = new DynamoDbDataSourceCursor()
                {
                    AfterToken = afterToken,
                    BeforeToken = token
                },
                Data = items
            };
        }

        protected async Task<List<T>> ScanAll(bool consistentRead)
        {
            var search = table.Scan(new ScanOperationConfig
            {
                ConsistentRead = consistentRead
            });

            var docs = await search.GetRemainingAsync();

            return docs
                .Select(d => ddbContext.FromDocument<T>(d))
                .ToList();
        }

        protected T ConvertToDoc(Dictionary<string, AttributeValue> dynamoDbImage)
        {
            var dynamoDocument = Document.FromAttributeMap(dynamoDbImage);
            return ddbContext.FromDocument<T>(dynamoDocument);
        }
    }
}
