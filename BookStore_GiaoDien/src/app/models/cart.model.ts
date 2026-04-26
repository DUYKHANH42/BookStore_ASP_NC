export interface CartItem {
  id: number;
  bookId: number;
  bookTitle: string;
  imageUrl: string;
  author: string;
  originalPrice: number;
  price: number;
  quantity: number;
  subTotal: number;
}

export interface Cart {
  id: number;
  items: CartItem[];
  totalPrice: number;
  totalItems: number;
}
