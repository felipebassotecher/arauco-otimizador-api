using Amazon.S3.Model;
using Techer.Aws.Shared;
using Techer.Common.Domain.Interfaces;

namespace Techer.Aws.Storage;

public class S3Helper
{
    public static string PreSign(IEnvironmentVariables env, AwsStringResource resource, string key, int expiresInMinutes)
    {
        string urlString;

        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(resource.Region)))
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = resource.Get(env.GetEnvironmentEnum()),
                Key = key,
                Expires = DateTime.Now.AddMinutes(expiresInMinutes),
                Verb = Amazon.S3.HttpVerb.GET
            };

            try
            {
                urlString = client.GetPreSignedURL(request);
            }
            catch (Exception)
            {
                throw;
            }
        }

        return urlString;
    }

    public static string GetUploadUrl(IEnvironmentVariables env, AwsStringResource resource, string key, int expiresInMinutes)
    {
        string urlString;

        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(resource.Region)))
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = resource.Get(env.GetEnvironmentEnum()),
                Key = key,
                Expires = DateTime.Now.AddMinutes(expiresInMinutes),
                Verb = Amazon.S3.HttpVerb.PUT
            };

            try
            {
                urlString = client.GetPreSignedURL(request);
            }
            catch (Exception)
            {
                throw;
            }
        }

        return urlString;
    }

    public static async Task UploadFile(IEnvironmentVariables env, AwsStringResource resource, string key, string filePath, Dictionary<string, string> metadata = null, Dictionary<string, string> tags = null)
    {
        await UploadFile(
            resource.Region,
            resource.Get(env.GetEnvironmentEnum()),
            key,
            filePath,
            metadata,
            tags);
    }

    public static async Task UploadFile(string region, string bucket, string key, string filePath, Dictionary<string, string> metadata = null, Dictionary<string, string> tags = null)
    {
        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region)))
        {
            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                FilePath = filePath
            };

            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    request.Metadata.Add(item.Key, item.Value);
                }
            }

            if (tags != null)
            {
                foreach (var item in tags)
                {
                    request.TagSet.Add(new Tag()
                    {
                        Key = item.Key,
                        Value = item.Value
                    });
                }
            }

            var response = await client.PutObjectAsync(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.HttpStatusCode.ToString());
        }
    }

    public static async Task UploadFile(string region, string bucket, string key, Stream stream, string contentType = null, Dictionary<string, string> metadata = null)
    {
        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region)))
        {
            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType
            };

            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    request.Metadata.Add(item.Key, item.Value);
                }
            }

            var response = await client.PutObjectAsync(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.HttpStatusCode.ToString());
        }
    }

    public static async Task UploadFile(string region, string bucket, string key, byte[] content, string contentType = null, Dictionary<string, string> metadata = null)
    {
        using (MemoryStream stream = new MemoryStream(content))
        {
            await UploadFile(region, bucket, key, stream, contentType, metadata);
        }
    }

    public static async Task UploadFile(IEnvironmentVariables env, AwsStringResource resource, string key, Stream stream, string contentType = null, Dictionary<string, string> metadata = null)
    {
        await UploadFile(
            resource.Region,
            resource.Get(env.GetEnvironmentEnum()),
            key,
            stream,
            contentType,
            metadata);
    }

    public static async Task UploadFile(IEnvironmentVariables env, AwsStringResource resource, string key, byte[] content, string contentType = null, Dictionary<string, string> metadata = null)
    {
        using (var stream = new MemoryStream(content))
        {
            await UploadFile(env, resource, key, stream, contentType, metadata);
        }
    }

    public static async Task PutFile(IEnvironmentVariables env, AwsStringResource bucket, string key, string content, Dictionary<string, string> metadata, string contentType = null)
    {
        await PutFile(bucket.Region, bucket.Get(env.GetEnvironmentEnum()), key, content, metadata, contentType);
    }

    public static async Task PutFile(string region, string bucket, string key, string content, Dictionary<string, string> metadata, string contentType = null)
    {
        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region)))
        {
            var request = new Amazon.S3.Model.PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                ContentBody = content,
                ContentType = contentType
            };

            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    request.Metadata.Add(item.Key, item.Value);
                }
            }

            var response = await client.PutObjectAsync(request);
        }
    }

    public static async Task<long> DownloadFile(IEnvironmentVariables env, AwsStringResource bucket, string key, string filePath)
    {
        return await DownloadFile(bucket.Region, bucket.Get(env.GetEnvironmentEnum()), key, filePath);
    }

    public static async Task<long> DownloadFile(string region, string bucket, string key, string filePath)
    {
        long length = 0;

        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region)))
        {
            var request = new GetObjectRequest
            {
                BucketName = bucket,
                Key = key,
            };

            var response = await client.GetObjectAsync(request);

            await response.WriteResponseStreamToFileAsync(filePath, false, System.Threading.CancellationToken.None);

            length = response.ContentLength;
        }

        return length;
    }

    public static async Task<string> GetFileAsString(IEnvironmentVariables env, AwsStringResource bucket, string key, System.Text.Encoding encoding)
    {
        return await GetFileAsString(
            bucket.Region,
            bucket.Get(env.GetEnvironmentEnum()),
            key,
            encoding);
    }

    public static async Task<string> GetFileAsString(string region, string bucket, string key, System.Text.Encoding encoding)
    {
        var conteudo = await GetFile(region, bucket, key);

        if (conteudo == null)
            return null;

        using (var reader = new StreamReader(conteudo, encoding, true, 1 << 20))
        {
            return reader.ReadToEnd();
        }
    }

    public static async Task<Stream> GetFile(IEnvironmentVariables env, AwsStringResource bucket, string key)
    {
        return await GetFile(bucket.Region, bucket.Get(env.GetEnvironmentEnum()), key);
    }

    public static async Task<Stream> GetFile(string region, string bucket, string key)
    {
        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region)))
        {
            var request = new GetObjectRequest
            {
                BucketName = bucket,
                Key = key,
            };

            var response = await client.GetObjectAsync(request);
            return response.ResponseStream;
        }
    }

    public static async Task<Stream> ReadBlock(IEnvironmentVariables env, AwsStringResource bucket, string key, long start, long end)
    {
        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(bucket.Region)))
        {
            var request = new GetObjectRequest
            {
                BucketName = bucket.Get(env.GetEnvironmentEnum()),
                Key = key,
                ByteRange = new ByteRange(start, end)
            };

            var response = await client.GetObjectAsync(request);
            return response.ResponseStream;
        }
    }

    public static async Task<bool> CopyFile(IEnvironmentVariables env, AwsStringResource sourceBucket, string sourceKey, AwsStringResource destinationBucket, string destinationKey, Dictionary<string, string> metadata = null)
    {
        return await CopyFile(
            sourceBucket.Region,
            sourceBucket.Get(env.GetEnvironmentEnum()),
            sourceKey,
            destinationBucket.Get(env.GetEnvironmentEnum()),
            destinationKey,
            metadata);
    }

    public static async Task<bool> CopyFile(string region, string sourceBucket, string sourceKey, string destinationBucket, string destinationKey, Dictionary<string, string> metadata = null)
    {
        bool transferred = false;

        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region)))
        {
            var request = new CopyObjectRequest
            {
                SourceBucket = sourceBucket,
                SourceKey = sourceKey,
                DestinationBucket = destinationBucket,
                DestinationKey = destinationKey,
                MetadataDirective = Amazon.S3.S3MetadataDirective.COPY
            };

            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    request.Metadata.Add(item.Key, item.Value);
                }
            }

            var response = await client.CopyObjectAsync(request);

            transferred = response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }

        return transferred;
    }

    public static async Task<bool> DeleteFile(IEnvironmentVariables env, AwsStringResource bucket, string key)
    {
        return await DeleteFile(
            bucket.Region,
            bucket.Get(env.GetEnvironmentEnum()),
            key);
    }

    public static async Task<bool> DeleteFile(string region, string bucket, string key)
    {
        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region)))
        {
            DeleteObjectRequest request = new DeleteObjectRequest()
            {
                BucketName = bucket,
                Key = key
            };

            // try to put the acl of the object
            DeleteObjectResponse response = await client.DeleteObjectAsync(request);

            return response.HttpStatusCode == System.Net.HttpStatusCode.OK || response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
        }
    }

    public static async Task<bool> CheckIfExists(IEnvironmentVariables env, AwsStringResource bucket, string key)
    {
        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(bucket.Region)))
        {
            var request = new GetObjectMetadataRequest()
            {
                BucketName = bucket.Get(env.GetEnvironmentEnum()),
                Key = key
            };

            try
            {
                var response = await client.GetObjectMetadataAsync(request);
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;

                throw;
            }

            return true;
        }
    }

    public static async Task<bool> CheckIfExists(string region, string bucket, string key)
    {
        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region)))
        {
            var request = new GetObjectMetadataRequest()
            {
                BucketName = bucket,
                Key = key
            };

            try
            {
                var response = await client.GetObjectMetadataAsync(request);
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;

                throw;
            }

            return true;
        }
    }

    public static async Task<long> GetObjectSize(string region, string bucket, string key)
    {
        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region)))
        {
            GetObjectMetadataRequest request = new GetObjectMetadataRequest()
            {
                BucketName = bucket,
                Key = key
            };

            var response = await client.GetObjectMetadataAsync(request);

            return response.Headers.ContentLength;
        }
    }

    public static async Task<Dictionary<string, string>> GetObjectMetadata(IEnvironmentVariables env, AwsStringResource bucket, string key)
    {
        return await GetObjectMetadata(
            bucket.Region,
            bucket.Get(env.GetEnvironmentEnum()),
            key);
    }

    public static async Task<Dictionary<string, string>> GetObjectMetadata(string region, string bucket, string key)
    {
        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region)))
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            GetObjectMetadataRequest request = new GetObjectMetadataRequest()
            {
                BucketName = bucket,
                Key = key
            };

            var response = await client.GetObjectMetadataAsync(request);

            foreach (var item in response.Headers.Keys)
            {
                res.Add(item, response.Headers[item]);
            }

            foreach (var item in response.Metadata.Keys)
            {
                res.Add(item.Substring(11), response.Metadata[item]);
            }

            return res;
        }
    }

    public static async Task<bool> DeleteFiles(IEnvironmentVariables env, AwsStringResource resource, string prefix)
    {
        var bucket = resource.Get(env.GetEnvironmentEnum());

        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(resource.Region)))
        {
            var listResponse = await client.ListObjectsAsync(new ListObjectsRequest
            {
                BucketName = bucket,
                Prefix = prefix
            });

            if (listResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                return false;

            if (listResponse.S3Objects.Count > 0)
            {
                var deleteResponse = await client.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = bucket,
                    Objects = listResponse.S3Objects.Select(s => new KeyVersion() { Key = s.Key }).ToList()
                });

                if (deleteResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    return false;
            }
        }

        return true;
    }

    public static async Task<bool> CreateFolder(IEnvironmentVariables env, AwsStringResource resource, string path, bool deleteIfExists)
    {
        var bucket = resource.Get(env.GetEnvironmentEnum());

        if (!path.EndsWith("/"))
            path += "/";

        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(resource.Region)))
        {
            if (deleteIfExists)
                await DeleteFiles(env, resource, path);

            var res = await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = path
            });

            return res.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
    }

    public static async Task<List<string>> ListObjects(IEnvironmentVariables env, AwsStringResource resource, string prefix)
    {
        var objects = new List<S3Object>();
        string continuationToken = null;

        using (var client = new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(resource.Region)))
        {
            do
            {
                var response = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = resource.Get(env.GetEnvironmentEnum()),
                    Prefix = prefix,
                    ContinuationToken = continuationToken
                });

                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK || response?.S3Objects == null)
                    throw new Exception("Não foi possível consultar os arquivos.");

                objects.AddRange(response.S3Objects);
                continuationToken = response.NextContinuationToken;

            } while (continuationToken != null);

            return objects
                .Select(o => o.Key)
                .ToList();
        }
    }
}
