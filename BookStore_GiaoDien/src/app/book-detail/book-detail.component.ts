import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BookService } from '../../services/book.service';
import { FavoriteService } from '../../services/favorite.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { CartService } from '../../services/cart.service';
import { Router } from '@angular/router';
import { Book } from '../models/book.model';
import { finalize, forkJoin, of, Subject, takeUntil } from 'rxjs';
import { catchError } from 'rxjs/operators';


@Component({
  selector: 'app-book-detail',
  templateUrl: './book-detail.component.html',
  styleUrls: ['./book-detail.component.css']
})
export class BookDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private bookService = inject(BookService);
  private favoriteService = inject(FavoriteService);
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private cartService = inject(CartService);
  private router = inject(Router);

  addToCart() {
    if (!this.book) return;
    
    this.cartService.addToCart(this.book.id, this.quantity).subscribe({
      next: () => {
        this.toastService.show('Đã thêm sản phẩm vào giỏ hàng!', 'success');
      },
      error: (err) => {
        console.error('Add to cart error:', err);
        this.toastService.show('Không thể thêm vào giỏ hàng. Vui lòng thử lại.', 'error');
      }
    });
  }

  buyNow() {
    if (!this.book) return;
    
    this.cartService.addToCart(this.book.id, this.quantity).subscribe({
      next: () => {
        this.router.navigate(['/checkout']);
      },
      error: (err) => {
        console.error('Buy now error:', err);
        this.toastService.show('Có lỗi xảy ra. Vui lòng thử lại.', 'error');
      }
    });
  }

  book: Book | null = null;
  isToggling = false;
  togglingId: number | null = null;
  private destroy$ = new Subject<void>();

  toggleFavorite(targetBook?: Book) {
    const b = targetBook || this.book;
    if (!b) return;
    
    if (!this.authService.isLoggedIn()) {
      this.toastService.show('Vui lòng đăng nhập để lưu vào danh sách yêu thích!', 'warning');
      this.router.navigate(['/login']);
      return;
    }

    if (!targetBook) this.isToggling = true;
    else this.togglingId = b.id;

    this.favoriteService.toggleFavorite(b.id).pipe(
      finalize(() => {
        if (!targetBook) this.isToggling = false;
        else this.togglingId = null;
      })
    ).subscribe({
      next: (res: any) => {
        const isFav = res.isFavorited ?? (res as any).IsFavorited;
        // isFavorited sẽ được cập nhật tự động qua subscription
        if (isFav) {
          this.toastService.show('Đã thêm thẻ sách vào Yêu thích! 💖', 'success');
        } else {
          this.toastService.show('Đã bỏ sách khỏi Yêu thích.', 'info');
        }
      },
      error: (err) => {
        console.error('Favorite toggle error:', err);
        this.toastService.show('Có lỗi xảy ra, vui lòng thử lại sau.', 'error');
      }
    });
  }
  relatedBooks: Book[] = [];
  quantity: number = 1;
  isLoading: boolean = false;
  activeTab: 'description' | 'details' | 'reviews' = 'description';
  myFavIds: number[] = [];

  ngOnInit() {
    this.route.params.subscribe(params => {
      const id = +params['id'];
      if (id) {
        this.loadBookDetail(id);
      }
    });
    this.initFavoriteSubscription();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  initFavoriteSubscription() {
    this.favoriteService.favoriteIds$
      .pipe(takeUntil(this.destroy$))
      .subscribe(ids => {
        if (this.book) {
          this.book.isFavorited = ids.includes(this.book.id);
        }
        // Cập nhật cả sách liên quan
        this.relatedBooks.forEach(b => {
          b.isFavorited = ids.includes(b.id);
        });
      });
  }

  loadBookDetail(id: number) {
    this.isLoading = true;
    
    const userFavorites$ = this.authService.isLoggedIn() 
      ? this.favoriteService.getFavorites().pipe(catchError(() => of([])))
      : of([]);

    forkJoin({
      bookData: this.bookService.getBookById(id),
      userFavorites: userFavorites$
    }).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (res: any) => {
        let rawData = res.bookData;
        if (rawData && rawData.$values && Array.isArray(rawData.$values)) {
          rawData = rawData.$values[0];
        } else if (rawData && rawData.data) {
          rawData = rawData.data;
        }

        if (rawData) {
          this.book = this.mapBook(rawData);
          
          // Đồng bộ trạng thái Yêu Thích ngay khi load
          let favData = res.userFavorites;
          if (favData && favData.$values && Array.isArray(favData.$values)) {
            favData = favData.$values;
          } else if (!Array.isArray(favData)) {
            favData = [];
          }
          
          const favIds = favData.map((f: any) => f.bookId || f.BookId);
          this.myFavIds = favIds; // Lưu trữ để tái sử dụng
          
          
          if (favIds.includes(this.book.id)) {
            this.book.isFavorited = true;
          }
          
          if (this.book.categoryId) {
            this.loadRelatedBooks(this.book.categoryId);
          }
        }
      },
      error: (err) => {
        console.error('Error loading book detail:', err);
      }
    });
  }

  loadRelatedBooks(categoryId: number) {
    this.bookService.getBooksByCategory(categoryId, 1, 5).subscribe({
      next: (res: any) => {
        let items: any[] = [];
        if (res && res.$values) items = res.$values;
        else if (res && res.items) items = res.items;
        else if (Array.isArray(res)) items = res;

        this.relatedBooks = items
          .map(item => {
             let b = this.mapBook(item);
             if (this.myFavIds.includes(b.id)) {
               b.isFavorited = true;
             }
             return b;
          })
          .filter(b => b.id !== this.book?.id)
          .slice(0, 4);
      }
    });
  }

  private mapBook(rawData: any): Book {
    return {
      id: rawData.id ?? rawData.Id,
      title: rawData.title ?? rawData.Title,
      author: rawData.author ?? rawData.Author,
      price: rawData.price ?? rawData.Price,
      description: rawData.description ?? rawData.Description,
      quantity: rawData.quantity ?? rawData.Quantity,
      imageUrl: rawData.imageUrl ?? rawData.ImageUrl,
      discountPrice: rawData.discountPrice ?? rawData.DiscountPrice,
      categoryName: rawData.categoryName ?? rawData.CategoryName,
      subCategoryName: rawData.subCategoryName ?? rawData.SubCategoryName,
      sku: rawData.sku ?? rawData.Sku,
      categoryId: rawData.categoryId ?? rawData.CategoryId,
      subCategoryId: rawData.subCategoryId ?? rawData.SubCategoryId,
      createdAt: rawData.createdAt ?? rawData.CreatedAt,
      isFlashSale: rawData.isFlashSale ?? rawData.IsFlashSale,
      isFavorited: rawData.isFavorited ?? rawData.IsFavorited
    } as Book;
  }

  incrementQuantity() {
    if (this.book && this.quantity < this.book.quantity) {
      this.quantity++;
    }
  }

  decrementQuantity() {
    if (this.quantity > 1) {
      this.quantity--;
    }
  }

  setTab(tab: 'description' | 'details' | 'reviews') {
    this.activeTab = tab;
  }
}
