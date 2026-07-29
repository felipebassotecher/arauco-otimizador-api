using Arauco.Otimizador.Aws.Shared;
using Arauco.Otimizador.Common.Domain.Constants;
using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Events;
using Arauco.Otimizador.Common.Email;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Colaborador;
using Arauco.Otimizador.Service.Base;
using Techer.Aws.Cognito;
using Techer.Aws.Cognito.Models;
using Techer.Aws.Shared;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Id;

namespace Arauco.Otimizador.Service.CognitoService
{
    public class CognitoService : ServiceBase
    {
        private CognitoHelper cognitoHelper;

        public CognitoService(IUnitOfWork unitOfWork, IEnvironmentVariables environmentVariables) : base(unitOfWork, environmentVariables)
        {
            cognitoHelper = new CognitoHelper(environmentVariables, Cognitos.App);
        }

        public async Task CreateAsync(Colaborador colaborador, string tempPassword)
        {
            AwsResource<CognitoData> cognito = Cognitos.App;

            string? subject = "Arauco Hub | Criação usuário";
            string? url = $"{AppDomains.GetAuthDomain(environmentVariables)}/login/entra?app=1&originalUrl={AppDomains.GetAuthDomain(environmentVariables)}";

            try
            {
                cognitoHelper = new CognitoHelper(environmentVariables, cognito);

                await cognitoHelper.CreateAsync(
                    colaborador.ColaboradorId.ToString(),
                    colaborador.Cpf,
                    tempPassword
                );

                var mailEvent = new MailEvent
                {
                    Subject = subject,
                    To = new List<AddressMailModel>
                    {
                        new AddressMailModel(colaborador.EmailComercial)
                    },
                    TemplateEnum = TemplateEmailEnum.NovaSenha,
                    Model = new Dictionary<string, string>
                    {
                        { "nome", colaborador.Nome },
                        { "nomeUsuario", colaborador.Cpf },
                        { "novaSenha", tempPassword },
                        { "url", url }
                    }
                };

                await EmailManager.SendAsync(environmentVariables, mailEvent);
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<string> ChangePasswordAsync(Colaborador colaborador, string email)
        {
            AwsResource<CognitoData> cognito = Cognitos.App;

            string? subject = "Arauco Hub | Nova senha";
            string? url = $"{AppDomains.GetAuthDomain(environmentVariables)}/login/entra?app=1&originalUrl={AppDomains.GetAuthDomain(environmentVariables)}";

            try
            {
                cognitoHelper = new CognitoHelper(environmentVariables, cognito);

                var novaSenha = await IdGenerator.New(8, IdGenerator.LetterAndNumberAlphabet);

                await cognitoHelper.ChangePasswordAsync($"COLAB#{colaborador.ColaboradorId}", novaSenha, true);

                var mailEvent = new MailEvent
                {
                    Subject = subject,
                    To = new List<AddressMailModel>
                    {
                        new AddressMailModel(email)
                    },
                    TemplateEnum = TemplateEmailEnum.NovaSenha,
                    Model = new Dictionary<string, string>
                    {
                        { "nome", colaborador.Nome },
                        { "nomeUsuario", colaborador.Cpf },
                        { "novaSenha", novaSenha },
                        { "url", url }
                    }
                };

                await EmailManager.SendAsync(environmentVariables, mailEvent);

                return novaSenha;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}