using System;

namespace BookStore.Domain.Common
{
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }

    public class NotFoundException : BusinessException
    {
        public NotFoundException(string resource, object key)
            : base($"{resource} with id '{key}' was not found.") { }
        public NotFoundException(string message) : base(message) { }
    }

    public class InsufficientStockException : BusinessException
    {
        public InsufficientStockException(string productName)
            : base($"Sản phẩm '{productName}' không đủ số lượng trong kho.") { }

        public InsufficientStockException(string productName, int requested, int available)
            : base($"Sản phẩm {productName} không đủ số lượng. Yêu cầu: {requested}, Còn: {available}") { }
    }

    public class ConflictException : BusinessException
    {
        public ConflictException(string message) : base(message) { }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message = "You do not have permission to perform this action.")
            : base(message) { }
    }

    public class ConcurrencyException : BusinessException
    {
        public ConcurrencyException()
            : base("Dữ liệu đã bị thay đổi bởi một tiến trình khác. Vui lòng thử lại.") { }
    }
}
