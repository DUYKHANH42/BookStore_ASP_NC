import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BookService } from '../../services/book.service';
import { CategoryService } from '../../services/category.service';
import { SubCategoryService } from '../../services/subcategory.service';
import { Book } from '../models/book.model';
import { Category } from '../models/category.model';
import { SubCategory } from '../models/subcategory.model';
import { finalize, forkJoin } from 'rxjs';

@Component({
  selector: 'app-product-listing',
  templateUrl: './product-listing.component.html'
})
export class ProductListingComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private bookService = inject(BookService);
  private categoryService = inject(CategoryService);
  private subCategoryService = inject(SubCategoryService);

  books: Book[] = [];
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

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.currentId = +params['id'];
      const path = this.route.snapshot.url[0].path;
      this.currentType = path === 'category' ? 'category' : 'subcategory';
      this.currentPage = 1;
      this.loadData();
    });

    this.categoryService.getCategories().subscribe(res => {
      this.categories = this.ensureArray(res);
    });
  }

  loadData() {
    this.isLoading = true;
    this.books = []; 

    if (this.currentType === 'category') {
      forkJoin({
        allCats: this.categoryService.getCategories(),
        allSubCats: this.subCategoryService.getSubcategories(),
        pagedResult: this.bookService.getBooksByCategory(this.currentId, this.currentPage, this.pageSize)
      }).pipe(
        finalize(() => this.isLoading = false)
      ).subscribe({
        next: (res) => {
          const categories = this.ensureArray(res.allCats);
          const cat = categories.find(c => c.id === this.currentId);
          this.title = cat ? (cat.name || (cat as any).Name) : 'Danh mục';
          
          this.parentCategory = null; // Ở cấp Category thì không có cha
          this.processPagedResult(res.pagedResult);
          
          const subCategories = this.ensureArray(res.allSubCats);
          this.subCategories = subCategories.filter(s => {
            const sCatId = s.categoryId || (s as any).CategoryId;
            return sCatId === this.currentId;
          });
        },
        error: (err) => console.error('Category load error:', err)
      });
    } else {
      forkJoin({
        allCats: this.categoryService.getCategories(),
        allSubCats: this.subCategoryService.getSubcategories(),
        pagedResult: this.bookService.getBooksBySubcategory(this.currentId, this.currentPage, this.pageSize)
      }).pipe(
        finalize(() => this.isLoading = false)
      ).subscribe({
        next: (res) => {
          const categories = this.ensureArray(res.allCats);
          const subCategories = this.ensureArray(res.allSubCats);
          const sub = subCategories.find(s => s.id === this.currentId);
          this.title = sub ? (sub.name || (sub as any).Name) : 'Mục con';
          
          this.processPagedResult(res.pagedResult);
          
          const parentId = sub ? (sub.categoryId || (sub as any).CategoryId) : 0;
          this.parentCategory = categories.find(c => c.id === parentId) || null;
          
          this.subCategories = subCategories.filter(s => {
            const sCatId = s.categoryId || (s as any).CategoryId;
            return sCatId === parentId;
          });
        },
        error: (err) => console.error('Subcategory load error:', err)
      });
    }
  }

  private processPagedResult(res: any) {
    if (!res) return;
    
    const rawItems = res.items || res.Items || res; 
    this.books = this.ensureArray(rawItems);
    
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
}
