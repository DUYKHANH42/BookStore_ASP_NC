using BookStore.Application.DTO;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services
{
    public class ShippingAddressService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShippingAddressService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<AddressDTO>> GetUserAddressesAsync(string userId)
        {
            var list = await _unitOfWork.ShippingAddresses.GetByUserIdAsync(userId);
            return list.Select(x => MapToDto(x));
        }

        public async Task AddAddressAsync(string userId, AddressDTO dto)
        {
            var address = new ShippingAddress
            {
                UserId = userId,
                ReceiverName = dto.ReceiverName,
                PhoneNumber = dto.PhoneNumber,
                AddressLine = dto.AddressLine,
                IsDefault = dto.IsDefault
            };

            if (address.IsDefault) await _unitOfWork.ShippingAddresses.SetDefaultAsync(userId, 0);

            await _unitOfWork.ShippingAddresses.AddAsync(address);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateAddressAsync(string userId, AddressDTO dto)
        {
            var addr = await _unitOfWork.ShippingAddresses.GetByIdAsync(dto.Id);
            if (addr == null || addr.UserId != userId) return;

            addr.ReceiverName = dto.ReceiverName;
            addr.PhoneNumber = dto.PhoneNumber;
            addr.AddressLine = dto.AddressLine;

            if (dto.IsDefault && !addr.IsDefault)
                await _unitOfWork.ShippingAddresses.SetDefaultAsync(userId, addr.Id);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAddressAsync(string userId, int id)
        {
            var addr = await _unitOfWork.ShippingAddresses.GetByIdAsync(id);
            if (addr != null && addr.UserId == userId)
            {
                await _unitOfWork.ShippingAddresses.DeleteAsync(addr.Id);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private AddressDTO MapToDto(ShippingAddress a) => new AddressDTO
        {
            Id = a.Id,
            ReceiverName = a.ReceiverName,
            PhoneNumber = a.PhoneNumber,
            AddressLine = a.AddressLine,
            IsDefault = a.IsDefault
        };
    }
}
