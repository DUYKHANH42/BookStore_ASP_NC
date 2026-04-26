import { Component, OnInit, inject } from '@angular/core';
import { CartService } from '../../services/cart.service';
import { Cart, CartItem } from '../models/cart.model';
import { Observable } from 'rxjs';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-cart',
  templateUrl: './cart.component.html',
})
export class CartComponent implements OnInit {
  private cartService = inject(CartService);
  private toastService = inject(ToastService);

  cart$: Observable<Cart> = this.cartService.cart$;

  ngOnInit(): void {
    // Optionally trigger a fresh load if needed
    // this.cartService.loadCartFromServer();
  }

  updateQuantity(item: CartItem, delta: number) {
    const newQty = item.quantity + delta;
    if (newQty > 0) {
      this.cartService.updateQuantity(item.bookId, newQty).subscribe({
        next: () => this.toastService.show('Đã cập nhật số lượng', 'success'),
        error: () => this.toastService.show('Không thể cập nhật số lượng', 'error')
      });
    } else {
      this.removeItem(item);
    }
  }

  removeItem(item: CartItem) {
    if (confirm(`Bạn có chắc muốn xóa "${item.bookTitle}" khỏi giỏ hàng?`)) {
      this.cartService.removeFromCart(item.bookId).subscribe({
        next: () => this.toastService.show('Đã xóa sản phẩm', 'info'),
        error: () => this.toastService.show('Có lỗi xảy ra khi xóa', 'error')
      });
    }
  }

  clearAll() {
    if (confirm('Bạn có chắc muốn xóa toàn bộ giỏ hàng?')) {
      this.cartService.clearCart().subscribe({
        next: () => this.toastService.show('Đã xóa toàn bộ giỏ hàng', 'info'),
        error: () => this.toastService.show('Có lỗi xảy ra khi xóa giỏ hàng', 'error')
      });
    }
  }

}
