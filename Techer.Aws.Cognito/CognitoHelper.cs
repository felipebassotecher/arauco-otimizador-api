using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Techer.Aws.Cognito.Models;
using Techer.Aws.Shared;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;

namespace Techer.Aws.Cognito
{
    public class CognitoHelper
    {
        private readonly AmazonCognitoIdentityProviderClient client;
        private readonly CognitoData cognitoData;

        public CognitoHelper(IEnvironmentVariables env, AwsResource<CognitoData> resource)
        {
            cognitoData = resource.Get(env.GetEnvironmentEnum());

            var awsRegion = Amazon.RegionEndpoint.GetBySystemName(resource.Region);

            client = new AmazonCognitoIdentityProviderClient(awsRegion);
        }

        public CognitoHelper(CognitoData cognitoData, string region)
        {
            this.cognitoData = cognitoData;

            var awsRegion = Amazon.RegionEndpoint.GetBySystemName(region);

            client = new AmazonCognitoIdentityProviderClient(awsRegion);
        }

        public async Task<string> CreateAsync(string username, string preferredUsername, string password, Dictionary<string, string>? attributes = null)
        {
            var request = new AdminCreateUserRequest
            {
                UserPoolId = cognitoData.PoolId,
                Username = username,
                TemporaryPassword = password,
            };

            request.UserAttributes.Add(new AttributeType()
            {
                Name = "preferred_username",
                Value = preferredUsername
            });

            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    request.UserAttributes.Add(new AttributeType
                    {
                        Name = attr.Key,
                        Value = attr.Value
                    });
                }
            }

            var res = await client.AdminCreateUserAsync(request);

            if (res.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new ApiException("Não foi possível criar o usuário no Cognito.");

            return res.User.Attributes.First(a => a.Name == "sub").Value;
        }

        public async Task ChangePreferredUsername(string username, string preferredUsername)
        {
            var request = new AdminUpdateUserAttributesRequest()
            {
                UserPoolId = cognitoData.PoolId,
                Username = username
            };

            request.UserAttributes.Add(new AttributeType()
            {
                Name = "preferred_username",
                Value = preferredUsername
            });

            var res = await client.AdminUpdateUserAttributesAsync(request);

            if (res.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new ApiException("Não foi possível atualizar o usuário no Cognito.");
        }

        public async Task ChangePasswordAsync(string username, string newPassword, bool isTemporary)
        {
            var request = new AdminSetUserPasswordRequest
            {
                UserPoolId = cognitoData.PoolId,
                Permanent = !isTemporary,
                Username = username,
                Password = newPassword
            };

            AdminSetUserPasswordResponse res = null;

            try
            {
                res = await client.AdminSetUserPasswordAsync(request);
            }
            catch (UserNotFoundException)
            {
                throw new ApiException("Nome de usuário não encontrado.");
            }
            catch (NotAuthorizedException)
            {
                throw new ApiException("Sem permissão para definir a nova senha.");
            }
            catch (Exception ex)
            {
                throw new ApiException("Ocorreu um erro ao definir a senha do usuário.", ex);
            }

            if (res.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new ApiException("Ocorreu um erro ao definir a senha do usuário.");
        }

        public async Task<UserModel> GetUserAsync(string username)
        {
            var user = await client.AdminGetUserAsync(new AdminGetUserRequest
            {
                UserPoolId = cognitoData.PoolId,
                Username = username
            });

            if (user == null)
                return null;

            return new UserModel
            {
                Username = user.Username,
                CreateDate = user.UserCreateDate,
                LastModifiedDate = user.UserLastModifiedDate,
                Enabled = user.Enabled,
                Attributes = user
                                .UserAttributes?
                                .Select(a => KeyValuePair.Create(a.Name, a.Value))
                                .ToDictionary(a => a.Key, a => a.Value)
            };
        }

        public async Task<bool> IsTokenValidAsync(string jwtToken)
        {
            try
            {
                var request = new GetUserRequest
                {
                    AccessToken = jwtToken
                };

                var response = await client.GetUserAsync(request);

                // JWT token is valid and belongs to a user in Cognito user pool
                return true;
            }
            catch (Exception ex)
            {
                // JWT token is invalid or failed to validate
                Console.WriteLine($"Failed to validate JWT token: {ex.Message}");
                return false;
            }
        }

        public async Task<List<UserModel>> ListUsers(string? filter)
        {
            string paginationToken = null;

            var users = new List<UserModel>();

            do
            {
                var response = await client.ListUsersAsync(new ListUsersRequest
                {
                    UserPoolId = cognitoData.PoolId,
                    Filter = filter
                });

                if (response.Users != null)
                {
                    foreach (var user in response.Users)
                    {
                        users.Add(new UserModel
                        {
                            Username = user.Username,
                            CreateDate = user.UserCreateDate,
                            LastModifiedDate = user.UserLastModifiedDate,
                            Enabled = user.Enabled,
                            Attributes = user
                                .Attributes?
                                .Select(a => KeyValuePair.Create(a.Name, a.Value))
                                .ToDictionary(a => a.Key, a => a.Value)
                        });
                    }
                }

                paginationToken = response.PaginationToken;
            } while (paginationToken != null);

            return users;
        }

        public async Task SetStatus(string username, bool enabled)
        {
            if (enabled)
            {
                var res = await client.AdminEnableUserAsync(new AdminEnableUserRequest()
                {
                    UserPoolId = cognitoData.PoolId,
                    Username = username
                });

                if (res.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    throw new ApiException("Não foi possível definir o status no Cognito.");
            }
            else
            {
                var res = await client.AdminDisableUserAsync(new AdminDisableUserRequest
                {
                    UserPoolId = cognitoData.PoolId,
                    Username = username
                });

                if (res.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    throw new ApiException("Não foi possível definir o status no Cognito.");
            }
        }

        public async Task DeleteAsync(string username)
        {
            var res = await client.AdminDeleteUserAsync(new AdminDeleteUserRequest()
            {
                UserPoolId = cognitoData.PoolId,
                Username = username
            });

            if (res.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new ApiException("Não foi possível definir o status no Cognito.");
        }

        public async Task<AuthenticateModel> AuthenticateAsync(string username, string password)
        {
            var request = new AdminInitiateAuthRequest
            {
                AuthFlow = AuthFlowType.ADMIN_USER_PASSWORD_AUTH,// ALLOW_USER_SRP_AUTH,
                ClientId = cognitoData.AppClientId,
                UserPoolId = cognitoData.PoolId,

                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", username },
                    { "PASSWORD", password }
                }
            };

            AdminInitiateAuthResponse auth = null;

            try
            {
                auth = await client.AdminInitiateAuthAsync(request);
            }
            catch (UserNotFoundException)
            {
                throw new ApiException("Nome de usuário ou senha inválida.");
            }
            catch (NotAuthorizedException)
            {
                throw new ApiException("Nome de usuário ou senha inválida.");
            }
            catch (Exception ex)
            {
                throw new ApiException("Ocorreu um erro ao autenticar o usuário.", ex);
            }

            if (auth.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new ApiException("Ocorreu um erro ao autenticar o usuário.");

            return new AuthenticateModel
            {
                IdToken = auth.AuthenticationResult.IdToken,
                AccessToken = auth.AuthenticationResult.AccessToken,
                ExpiresIn = auth.AuthenticationResult.ExpiresIn,
                RefreshToken = auth.AuthenticationResult.RefreshToken
            };
        }
    }
}
