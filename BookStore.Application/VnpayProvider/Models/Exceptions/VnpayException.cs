using System;
using BookStore.Application.VnpayProvider.Models.Enums;

namespace BookStore.Application.VnpayProvider.Models.Exceptions
{
    public class VnpayException : Exception
    {
        public string Message { get; internal set; }
        public TransactionStatusCode TransactionStatusCode { get; internal set; }
        public PaymentResponseCode PaymentResponseCode { get; internal set; }
    }
}
