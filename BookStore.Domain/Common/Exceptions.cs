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
    }

    public class ConcurrencyException : BusinessException
    {
        public ConcurrencyException()
            : base("Dữ liệu đã bị thay đổi bởi một tiến trình khác. Vui lòng thử lại.") { }
    }
}
