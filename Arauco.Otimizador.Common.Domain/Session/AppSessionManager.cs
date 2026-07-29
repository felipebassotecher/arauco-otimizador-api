using Arauco.Otimizador.Common.Domain.Interfaces;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.Common.Domain.Session
{
    public class AppSessionManager : ISessionManager<AppSessionModel>
    {
        private readonly IKeyValueRepository keyValueRepository;

        public AppSessionManager(IKeyValueRepository keyValueRepository)
        {
            this.keyValueRepository = keyValueRepository;
        }

        public async Task AddAsync(string userId, AppSessionModel sessionModel)
        {
            await keyValueRepository.SaveAsync(
                GetKey(userId),
                sessionModel,
                DateTime.UtcNow.AddHours(24));
        }

        public async Task<AppSessionModel> GetAsync(string userId, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidSessionException();

            if (string.IsNullOrWhiteSpace(sessionId))
                throw new InvalidSessionException();

            AppSessionModel data;

            var key = GetKey(sessionId);
            (data, _) = await keyValueRepository.GetAsync<AppSessionModel>(key);

            if (data == null || data.SessionId == null)
                throw new InvalidSessionException();

            return data;
        }

        public async Task DeleteAsync(string userId)
        {
            await keyValueRepository.DeleteAsync(GetKey(userId));
        }

        private static string GetKey(string sessionId)
        {
            return $"SESSION_{sessionId}";
        }
    }
}
