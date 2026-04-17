import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { BookService } from '../../services/book.service';
import { CategoryService } from '../../services/category.service';
import { SubCategoryService } from '../../services/subcategory.service';
import { Book } from '../models/book.model';
import { Category } from '../models/category.model';
import { SubCategory } from '../models/subcategory.model';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit, OnDestroy {
  private bookService = inject(BookService);
  private categoryService = inject(CategoryService);
  private subCategoryService = inject(SubCategoryService);

  books: Book[] = [];
  categories: Category[] = [];
  subCategories: SubCategory[] = [];
  
  navCategories: Category[] = [];
  flashSaleBooks: Book[] = [];
  countdown = { hours: '00', minutes: '00', seconds: '00' };
  private countdownInterval: any;

  ngOnInit() {
    this.startCountdown();
    this.loadData();
  }

  ngOnDestroy() {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
  }

  loadData() {
    forkJoin({
      books: this.bookService.getBooks(),
      categories: this.categoryService.getCategories(),
      subCategories: this.subCategoryService.getSubcategories()
    }).subscribe({
      next: (result) => {
        this.books = result.books;
        this.subCategories = result.subCategories;
        this.categories = result.categories.map(cat => ({
          ...cat,
          subCategories: this.subCategories.filter(sub => sub.categoryId === cat.id)
        }));
        this.navCategories = this.categories;
        this.flashSaleBooks = this.books.filter(book => book.isFlashSale);
      },
      error: (err) => console.error('Lỗi khi tải dữ liệu Home:', err)
    });
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
}
