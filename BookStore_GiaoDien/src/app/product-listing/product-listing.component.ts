import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
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
import { PagedResult } from '../models/paged-result.model';
import { forkJoin, Observable, of, finalize } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-product-listing',
  templateUrl: './product-listing.component.html'
})
export class ProductListingComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private bookService = inject(BookService);
  private categoryService = inject(CategoryService);
  private subCategoryService = inject(SubCategoryService);
  private favoriteService = inject(FavoriteService);
  private authService = inject(AuthService);
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
  togglingId: number | null = null; // Quản lý loading cho từng nút tym

  toggleFavorite(book: Book) {
    if (!this.authService.isLoggedIn()) {
      this.toastService.show('Vui lòng đăng nhập để sử dụng tính năng này!', 'warning');
      this.router.navigate(['/login']);
      return;
    }

    this.togglingId = book.id;
    this.favoriteService.toggleFavorite(book.id).pipe(
      finalize(() => this.togglingId = null)
    ).subscribe({
      next: (res: any) => {
        const isFav = res.isFavorited ?? res.IsFavorited;
        book.isFavorited = isFav;
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
  title: string = '';
  description: string = 'Khám phá bộ sưu tập được tuyển chọn kỹ lưỡng.';
  
  categories: Category[] = [];
  subCategories: SubCategory[] = [];
  currentType: 'category' | 'subcategory' = 'category';
  currentId: number = 0;
  parentCategory: Category | null = null; // Thêm để làm Breadcrumbs

  // Loading State
  isLoading: boolean = false;

  // Pagination State
  currentPage: number = 1;
  totalPages: number = 1;
  pageSize: number = 5;
  totalItems: number = 0;

  // Filter & Sort State
  minPrice: number = 0;
  maxPrice: number = 1000000;
  sortBy: string = 'newest';
  searchTerm: string = '';

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.currentId = +params['id'];
      
      // Kiểm tra xem có phải trang Search không
      this.route.queryParams.subscribe(queryParams => {
        this.searchTerm = queryParams['q'] || '';
        
        const url = this.route.snapshot.url;
        if (url.length > 0 && url[0].path === 'search') {
          this.currentType = 'category'; // Coi như category để chạy logic chung nhưng logic loadData sẽ check searchTerm
          this.title = `Kết quả tìm kiếm cho "${this.searchTerm}"`;
          this.currentId = 0;
        } else {
          this.currentType = url.length > 0 && url[0].path === 'category' ? 'category' : 'subcategory';
        }
        
        this.resetFilters();
        this.loadData();
      });
    });

    this.categoryService.getCategories().subscribe(res => {
      this.categories = this.ensureArray(res);
    });
  }

  resetFilters() {
    this.currentPage = 1;
    this.minPrice = 0;
    this.maxPrice = 1000000;
    this.sortBy = 'newest';
  }

  onFilterChange() {
    this.currentPage = 1;
    this.loadData();
  }

  setPriceRange(min: number, max: number) {
    this.minPrice = min;
    this.maxPrice = max;
    this.onFilterChange();
  }

  onSortChange() {
    this.currentPage = 1;
    this.loadData();
  }

  loadData() {
    this.isLoading = true;
    this.books = []; 

    let request$: Observable<PagedResult<Book>>;

    if (this.searchTerm) {
      request$ = this.bookService.searchBooks(this.searchTerm, this.currentPage, this.pageSize, this.minPrice, this.maxPrice);
    } else {
      request$ = this.currentType === 'category' 
        ? this.bookService.getBooksByCategory(this.currentId, this.currentPage, this.pageSize, this.minPrice, this.maxPrice, this.sortBy)
        : this.bookService.getBooksBySubcategory(this.currentId, this.currentPage, this.pageSize, this.minPrice, this.maxPrice, this.sortBy);
    }

    const userFavorites$ = this.authService.isLoggedIn() 
      ? this.favoriteService.getFavorites().pipe(catchError(() => of([])))
      : of([]);

    forkJoin({
      allCats: this.categoryService.getCategories(),
      allSubCats: this.subCategoryService.getSubcategories(),
      pagedResult: request$,
      userFavorites: userFavorites$
    }).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (res) => {
        // Ánh xạ trạng thái yêu thích
        const favData = this.ensureArray(res.userFavorites);
        const favIds = favData.map(f => f.bookId || f.BookId);

        const categories = this.ensureArray(res.allCats);
        const subCategories = this.ensureArray(res.allSubCats);
        
        if (this.currentType === 'category') {
          const cat = categories.find(c => c.id === this.currentId);
          if (!this.searchTerm) {
            this.title = cat ? (cat.name || (cat as any).Name) : 'Danh mục';
            this.subCategories = subCategories.filter(s => (s.categoryId || (s as any).CategoryId) === this.currentId);
          }
          this.parentCategory = null;
        } else {
          const sub = subCategories.find(s => s.id === this.currentId);
          this.title = sub ? (sub.name || (sub as any).Name) : 'Mục con';
          const parentId = sub ? (sub.categoryId || (sub as any).CategoryId) : 0;
          this.parentCategory = categories.find(c => c.id === parentId) || null;
          this.subCategories = subCategories.filter(s => (s.categoryId || (s as any).CategoryId) === parentId);
        }

        this.processPagedResult(res.pagedResult, favIds);
      },
      error: (err) => console.error('Data load error:', err)
    });
  }

  private processPagedResult(res: any, favIds: number[]) {
    if (!res) return;
    
    const rawItems = res.items || res.Items || (res.$values) || []; 
    this.books = this.ensureArray(rawItems).map(b => {
      let mapped = this.mapBook(b);
      if (favIds.includes(mapped.id)) {
        mapped.isFavorited = true;
      }
      return mapped;
    });
    
    // Ép kiểu Number tuyệt đối để tránh lỗi so sánh chuỗi
    this.totalItems = Number(res.totalItems || res.TotalItems || 0);
    this.totalPages = Number(res.totalPages || res.TotalPages || 1);
    this.currentPage = Number(res.currentPage || res.CurrentPage || 1);
    this.pageSize = Number(res.pageSize || res.PageSize || this.pageSize);
  }

  // Hàm tiện ích để đảm bảo luôn trả về Mảng
  private ensureArray(data: any): any[] {
    if (!data) return [];
    if (Array.isArray(data)) return data;
    if (data.$values && Array.isArray(data.$values)) return data.$values;
    if (data.items && Array.isArray(data.items)) return data.items;
    return [];
  }

  changePage(page: number) {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.currentPage = page;
      this.loadData();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  getPages(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
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
