using Arauco.Otimizador.Aws.Shared;
using Arauco.Otimizador.Common.Domain.Events;
using Techer.Aws.Queue;
using Techer.Common.Domain.Enums;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Json;

namespace Arauco.Otimizador.Common.Email
{
    public static class EmailManager
    {
        public static async Task SendAsync(IEnvironmentVariables env, MailEvent model)
        {
            // Set the sender
            if (model.From == null)
            {
                switch (env.GetEnvironmentEnum())
                {
                    case EnvironmentEnum.Dev:
                        model.From = new AddressMailModel("Arauco", "noreply@dev.hub.arauco.app.br");
                        break;

                    case EnvironmentEnum.Test:
                        model.From = new AddressMailModel("Arauco", "noreply@test.hub.arauco.app.br");
                        break;

                    default:
                        model.From = new AddressMailModel("Arauco", "noreply@hub.arauco.app.br");
                        break;
                }
            }

            await SqsHelper.SendMessageStandard(
                env,
                Queues.Email,
                JsonHelper.Serialize(model),
                0);
        }
    }
}