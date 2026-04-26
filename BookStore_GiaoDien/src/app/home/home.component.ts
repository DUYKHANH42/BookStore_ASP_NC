import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { BookService } from '../../services/book.service';
import { CategoryService } from '../../services/category.service';
import { SubCategoryService } from '../../services/subcategory.service';
import { FavoriteService } from '../../services/favorite.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { CartService } from '../../services/cart.service';
import { Router } from '@angular/router';
import { Book } from '../models/book.model';
import { Category } from '../models/category.model';
import { SubCategory } from '../models/subcategory.model';
import { forkJoin, finalize, of, Subject, takeUntil } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit, OnDestroy {
  private bookService = inject(BookService);
  private categoryService = inject(CategoryService);
  private subCategoryService = inject(SubCategoryService);
  private favoriteService = inject(FavoriteService);
  public authService = inject(AuthService);
  private toastService = inject(ToastService);
  private cartService = inject(CartService);
  private router = inject(Router);

  addToCart(book: Book) {
    this.cartService.addToCart(book.id, 1).subscribe({
      next: () => {
        this.toastService.show(`Đã thêm "${book.title}" vào giỏ hàng!`, 'success');
      },
      error: (err) => {
        console.error('Add to cart error:', err);
        this.toastService.show('Không thể thêm vào giỏ hàng.', 'error');
      }
    });
  }

  books: Book[] = [];
  togglingId: number | null = null;

  toggleFavorite(book: Book) {
    if (!this.authService.isLoggedIn()) {
      this.toastService.show('Đăng nhập ngay để "thả tim" bạn nhé! ❤️', 'info');
      this.router.navigate(['/login']);
      return;
    }

    this.togglingId = book.id;
    this.favoriteService.toggleFavorite(book.id).pipe(
      finalize(() => this.togglingId = null)
    ).subscribe({
      next: (res: any) => {
        const isFav = res.isFavorited ?? (res as any).IsFavorited;
        // Không cần gán book.isFavorited ở đây nữa vì Subject sẽ lo việc đó
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
  categories: Category[] = [];
  subCategories: SubCategory[] = [];
  
  navCategories: Category[] = [];
  flashSaleBooks: Book[] = [];
  myFavoriteBooks: Book[] = []; // Chứa danh sách sách yêu thích người dùng
  countdown = { hours: '00', minutes: '00', seconds: '00' };
  private countdownInterval: any;
  private destroy$ = new Subject<void>();

  ngOnInit() {
    this.startCountdown();
    this.loadData();
    this.initFavoriteSubscription();
  }

  ngOnDestroy() {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Đăng ký nhận thay đổi danh sách yêu thích
  initFavoriteSubscription() {
    this.favoriteService.favoriteIds$
      .pipe(takeUntil(this.destroy$))
      .subscribe(ids => {
        this.syncFavoriteState(ids);
      });
  }

  syncFavoriteState(favIds: number[]) {
    // 1. Cập nhật flag cho tất cả sách
    this.books.forEach(b => {
      b.isFavorited = favIds.includes(b.id);
    });

    // 2. Cập nhật danh sách hiển thị ở mục "Sách bạn đã thích"
    this.myFavoriteBooks = this.books.filter(b => b.isFavorited);
    
    // Cập nhật flash sale nếu cần
    this.flashSaleBooks = this.books.filter(book => book.isFlashSale);
  }

  loadData() {
    const userFavorites$ = this.authService.isLoggedIn() 
      ? this.favoriteService.getFavorites().pipe(catchError(() => of([])))
      : of([]);

    forkJoin({
      booksPaged: this.bookService.getBooksPaged(1, 20, undefined, undefined, 'newest'),
      categories: this.categoryService.getCategories(),
      subCategories: this.subCategoryService.getSubcategories(),
      userFavorites: userFavorites$
    }).subscribe({
      next: (result) => {
        // Trích xuất danh sách ID sách yêu thích
        const favData = this.ensureArray(result.userFavorites);
        const favIds = favData.map(f => f.bookId || f.BookId);

        // Handle PagedResult & Mapping
        const rawBooks = result.booksPaged.items || (result.booksPaged as any).$values || [];
        this.books = rawBooks.map((b: any) => {
          let mappedBook = this.mapBook(b);
          if (favIds.includes(mappedBook.id)) {
            mappedBook.isFavorited = true;
          }
          return mappedBook;
        });
        
        this.myFavoriteBooks = this.books.filter(b => b.isFavorited);

        this.subCategories = this.ensureArray(result.subCategories);
        
        this.categories = this.ensureArray(result.categories).map(cat => ({
          ...cat,
          subCategories: this.subCategories.filter(sub => (sub.categoryId || (sub as any).CategoryId) === cat.id)
        }));
        
        this.navCategories = this.categories;
        this.flashSaleBooks = this.books.filter(book => book.isFlashSale);
      },
      error: (err) => console.error('Lỗi khi tải dữ liệu Home:', err)
    });
  }

  // Tiện ích đảm bảo mảng
  private ensureArray(data: any): any[] {
    if (!data) return [];
    if (Array.isArray(data)) return data;
    if (data.$values && Array.isArray(data.$values)) return data.$values;
    return [];
  }

  startCountdown() {
    const endTime = new Date();
    endTime.setHours(endTime.getHours() + 5); 

    this.countdownInterval = setInterval(() => {
      const now = new Date().getTime();
      const distance = endTime.getTime() - now;

      if (distance < 0) {
        clearInterval(this.countdownInterval);
        return;
      }

      const h = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
      const m = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
      const s = Math.floor((distance % (1000 * 60)) / 1000);

      this.countdown = {
        hours: h.toString().padStart(2, '0'),
        minutes: m.toString().padStart(2, '0'),
        seconds: s.toString().padStart(2, '0')
      };
    }, 1000);
  }

  getBooksByCategory(categoryId: number): Book[] {
    return this.books.filter(book => book.categoryId === categoryId).slice(0, 4);
  }

  getCategoryIcon(name: string): string {
    const iconMap: { [key: string]: string } = {
      'Văn Học': 'history_edu',
      'Khoa Học & Công Nghệ': 'biotech',
      'Kỹ Năng Sống': 'psychology',
      'Kinh Tế': 'payments',
      'Nghệ Thuật': 'palette',
      'Sổ Tay': 'auto_stories',
      'Văn Phòng Phẩm': 'edit_note'
    };
    return iconMap[name] || 'book_2';
  }

  private mapBook(rawData: any): Book {
    return {
      ...rawData,
      id: rawData.id ?? rawData.Id,
      title: rawData.title ?? rawData.Title,
      author: rawData.author ?? rawData.Author,
      price: rawData.price ?? rawData.Price,
      imageUrl: rawData.imageUrl ?? rawData.ImageUrl,
      discountPrice: rawData.discountPrice ?? rawData.DiscountPrice,
      categoryId: rawData.categoryId ?? rawData.CategoryId,
      isFavorited: rawData.isFavorited ?? rawData.IsFavorited ?? false
    } as Book;
  }
}
