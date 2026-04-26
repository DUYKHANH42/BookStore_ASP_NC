import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { BookService } from '../services/book.service';
import { CategoryService } from '../services/category.service';
import { SubCategoryService } from '../services/subcategory.service';
import { Book } from './models/book.model';
import { Category } from './models/category.model';
import { SubCategory } from './models/subcategory.model';
import { forkJoin } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';
import { CartService } from '../services/cart.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  public authService = inject(AuthService);
  public toastService = inject(ToastService);
  public cartService = inject(CartService);
  private categoryService = inject(CategoryService);
  private subCategoryService = inject(SubCategoryService);
  private bookService = inject(BookService);
  private router = inject(Router);

  categories: Category[] = [];
  subCategories: SubCategory[] = [];
  navCategories: Category[] = [];
  
  toast$ = this.toastService.toastState$;
  cart$ = this.cartService.cart$;

  // Live Search State
  searchTerm: string = '';
  suggestions: Book[] = [];
  searchTimeout: any;

  // Trạng thái đóng mở Menu Mobile
  isMenuOpen = false;

  onSearchInput() {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }

    if (!this.searchTerm.trim()) {
      this.suggestions = [];
      return;
    }

    this.searchTimeout = setTimeout(() => {
      this.bookService.searchBooks(this.searchTerm, 1, 5).subscribe({
        next: (res) => {
          this.suggestions = res.items || (res as any).$values || [];
        },
        error: (err) => console.error('Search error:', err)
      });
    }, 300); // 300ms debounce
  }

  onSelectBook(bookId: number) {
    this.suggestions = [];
    this.searchTerm = '';
    this.router.navigate(['/book', bookId]);
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/']);
  }

  onKeywordSearch() {
    if (!this.searchTerm.trim()) return;
    
    const query = this.searchTerm;
    this.suggestions = [];
    this.searchTerm = '';
    this.router.navigate(['/search'], { queryParams: { q: query } });
  }

  closeSearch() {
    // Để timeout nhẹ để kịp nhận sự kiện click vào item
    setTimeout(() => {
      this.suggestions = [];
    }, 200);
  }

  constructor() { }

  toggleMenu() {
    this.isMenuOpen = !this.isMenuOpen;
  }

  ngOnInit() {
    forkJoin({
      categories: this.categoryService.getCategories(),
      subCategories: this.subCategoryService.getSubcategories()
    }).subscribe({
      next: (result) => {
        const categories = this.ensureArray(result.categories);
        const subCategories = this.ensureArray(result.subCategories);
        
        this.categories = categories.map(cat => ({
          ...cat,
          subCategories: subCategories.filter(sub => (sub.categoryId || (sub as any).CategoryId) === cat.id)
        }));
        this.navCategories = this.categories;
      },
      error: (err) => console.error('Navbar load error:', err)
    });
  }

  // Hàm tiện ích để đảm bảo luôn trả về Mảng
  private ensureArray(data: any): any[] {
    if (!data) return [];
    if (Array.isArray(data)) return data;
    if (data.$values && Array.isArray(data.$values)) return data.$values;
    return [];
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
}
