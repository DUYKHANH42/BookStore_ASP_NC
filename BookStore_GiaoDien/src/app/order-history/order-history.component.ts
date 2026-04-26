import { Component, OnInit, inject } from '@angular/core';
import { OrderService } from '../../services/order.service';
import { Order, OrderFullDetail } from '../models/order.model';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-order-history',
  templateUrl: './order-history.component.html'
})
export class OrderHistoryComponent implements OnInit {
  private orderService = inject(OrderService);
  private toastService = inject(ToastService);

  orders: Order[] = [];
  isLoading = false;
  selectedOrder: OrderFullDetail | null = null;
  showDetail = false;

  ngOnInit() {
    this.loadOrders();
  }

  loadOrders() {
    this.isLoading = true;
    this.orderService.getHistory().subscribe({
      next: (res) => {
        this.orders = res;
        this.isLoading = false;
      },
      error: () => {
        this.toastService.show('Không thể tải lịch sử đơn hàng', 'error');
        this.isLoading = false;
      }
    });
  }

  viewDetail(orderId: number) {
    this.orderService.getOrderDetails(orderId).subscribe({
      next: (res) => {
        this.selectedOrder = res;
        this.showDetail = true;
      },
      error: (err) => {
        if (err.status === 403) {
          this.toastService.show('Bạn không có quyền xem đơn hàng này', 'error');
        } else {
          this.toastService.show('Không thể tải chi tiết đơn hàng', 'error');
        }
      }
    });
  }

  closeDetail() {
    this.showDetail = false;
    this.selectedOrder = null;
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'chờ xác nhận': return 'bg-yellow-50 text-yellow-600 border-yellow-100';
      case 'đã xác nhận': return 'bg-blue-50 text-blue-600 border-blue-100';
      case 'đang giao': return 'bg-purple-50 text-purple-600 border-purple-100';
      case 'đã giao': return 'bg-green-50 text-green-600 border-green-100';
      case 'đã hủy': return 'bg-red-50 text-red-600 border-red-100';
      default: return 'bg-slate-50 text-slate-600 border-slate-100';
    }
  }
}
