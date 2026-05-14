using System;

namespace BookStore.Application.Interfaces
{
    public interface IDateTimeProvider
    {
        DateTime VnNow { get; }
    }
}
