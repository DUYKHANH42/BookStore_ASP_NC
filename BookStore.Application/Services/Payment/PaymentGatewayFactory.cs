using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace BookStore.Application.Services.Payment
{
    public class PaymentGatewayFactory
    {
        private readonly IEnumerable<IPaymentGateway> _gateways;

        public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
        {
            _gateways = gateways;
        }

        public IPaymentGateway? GetGateway(PaymentMethod method)
        {
            return _gateways.FirstOrDefault(g => g.SupportedMethod == method);
        }
    }
}
