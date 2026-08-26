using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
  public class ShippingAddressRepository : GenericRepository<ShippingAddress>, IShippingAddressRepository
{
    public ShippingAddressRepository(BookStoreDbContext context) : base(context) { }

    public async Task<IEnumerable<ShippingAddress>> GetByUserIdAsync(string userId)
    {
        return await _context.ShippingAddresses
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }

    public async Task SetDefaultAsync(string userId, int addressId)
    {
        var addresses = await _context.ShippingAddresses.Where(x => x.UserId == userId).ToListAsync();
        foreach (var addr in addresses)
        {
            addr.IsDefault = (addr.Id == addressId);
        }
    }
}
}
