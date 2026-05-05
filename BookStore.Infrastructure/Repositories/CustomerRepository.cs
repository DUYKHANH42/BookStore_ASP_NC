using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Infrastructure.Persistence;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Infrastructure.Repositories
{
    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(BookStoreDbContext context) : base(context) { }

        public async Task<Customer> GetByUserIdAsync(string userId)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }
      
    }
}