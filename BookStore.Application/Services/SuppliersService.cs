using BookStore.Domain.Interfaces;
using BookStore.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace BookStore.Application.Services
{
    public class SuppliersService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SuppliersService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            return await _unitOfWork.Suppliers.GetAllAsync();
        }
    }
}
