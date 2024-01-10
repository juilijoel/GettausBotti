﻿using GettausBotti.Interfaces.Models;
using Telegram.Bot.Types;

namespace GettausBotti.Interfaces.Services
{
    public interface IGettingService
    {
        Task<IGetResponse> TryGetAsync(Message message, CancellationToken cancellationToken);

        Task<IEnumerable<IGetScore>> GetScores(long paChatId, int topCount, int? paYear);

        Task<IEnumerable<IFameRow>> GetHallOfFame(long paChatId, int paStartingYear, int paCurrentYear);

        Task<bool> RemoveScores(long paUserId);
    }
}