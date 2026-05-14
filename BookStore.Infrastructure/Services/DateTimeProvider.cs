using BookStore.Application.Interfaces;
using BookStore.Domain.Common;
using System;

namespace BookStore.Infrastructure.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime VnNow => TimeHelper.GetVnTime();
    }
}
