using BookStore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Domain.Interfaces
{
    public interface IShippingAddressRepository : IGenericRepository<ShippingAddress>
    {
        Task<IEnumerable<ShippingAddress>> GetByUserIdAsync(string userId);
        Task SetDefaultAsync(string userId, int addressId);
    }
}
