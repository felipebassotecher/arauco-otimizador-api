using Arauco.Otimizador.Data.Entities;
using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Service.Base
{
    public class ServiceBase
    {
        protected readonly IUnitOfWork unitOfWork;
        protected readonly IEnvironmentVariables environmentVariables;

        public ServiceBase(IUnitOfWork unitOfWork, IEnvironmentVariables environmentVariables)
        {
            this.unitOfWork = unitOfWork;
            this.environmentVariables = environmentVariables;
        }
    }
}