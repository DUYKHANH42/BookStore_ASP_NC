import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { BookService } from '../services/book.service';
import { CategoryService } from '../services/category.service';
import { SubCategoryService } from '../services/subcategory.service';
import { Book } from './models/book.model';
import { Category } from './models/category.model';
import { SubCategory } from './models/subcategory.model';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  private categoryService = inject(CategoryService);
  private subCategoryService = inject(SubCategoryService);

  categories: Category[] = [];
  subCategories: SubCategory[] = [];
  navCategories: Category[] = [];

  // Trạng thái đóng mở Menu Mobile
  isMenuOpen = false;

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
        this.categories = result.categories.map(cat => ({
          ...cat,
          subCategories: result.subCategories.filter(sub => sub.categoryId === cat.id)
        }));
        this.navCategories = this.categories;
      },
      error: (err) => console.error('Navbar load error:', err)
    });
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
