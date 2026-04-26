export interface OrderItem {
  bookId: number;
  bookTitle: string;
  imageUrl: string;
  price: number;
  quantity: number;
  subTotal: number;
}

export interface Order {
  id: number;
  orderNumber: string;
  totalPrice: number;
  status: string;
  createdAt: string;
}

export interface OrderFullDetail extends Order {
  shippingName: string;
  shippingPhone: string;
  shippingAddress: string;
  items: OrderItem[];
}

export interface CheckoutDto {
  shippingName: string;
  shippingPhone: string;
  shippingAddress: string;
  paymentMethod: number; // 0: COD, 1: Credit Card
}
