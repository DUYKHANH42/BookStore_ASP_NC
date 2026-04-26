import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Book } from '../app/models/book.model';
import { PagedResult } from '../app/models/paged-result.model';

@Injectable({
  providedIn: 'root'
})

export class BookService {
  private apiUrl = `${environment.apiUrl}/books`;

  constructor(private http: HttpClient) { }

  getBooks(): Observable<Book[]> {
    return this.http.get<Book[]>(`${this.apiUrl}/all`);
  }

  getBooksPaged(page: number = 1, pageSize: number = 12, minPrice?: number, maxPrice?: number, sortBy?: string): Observable<PagedResult<Book>> {
    let params = new HttpParams()
      .set('Page', page.toString())
      .set('PageSize', pageSize.toString());
    
    if (minPrice != null) params = params.set('MinPrice', minPrice.toString());
    if (maxPrice != null) params = params.set('MaxPrice', maxPrice.toString());
    if (sortBy) params = params.set('SortBy', sortBy);

    return this.http.get<PagedResult<Book>>(this.apiUrl, { params });
  }

  getBooksByCategory(categoryId: number, page: number = 1, pageSize: number = 12, minPrice?: number, maxPrice?: number, sortBy?: string): Observable<PagedResult<Book>> {
    let params = new HttpParams()
      .set('Page', page.toString())
      .set('PageSize', pageSize.toString());
    
    if (minPrice != null) params = params.set('MinPrice', minPrice.toString());
    if (maxPrice != null) params = params.set('MaxPrice', maxPrice.toString());
    if (sortBy) params = params.set('SortBy', sortBy);

    return this.http.get<PagedResult<Book>>(`${this.apiUrl}/category/${categoryId}`, { params });
  }

  getBooksBySubcategory(subcategoryId: number, page: number = 1, pageSize: number = 12, minPrice?: number, maxPrice?: number, sortBy?: string): Observable<PagedResult<Book>> {
    let params = new HttpParams()
      .set('Page', page.toString())
      .set('PageSize', pageSize.toString());
    
    if (minPrice != null) params = params.set('MinPrice', minPrice.toString());
    if (maxPrice != null) params = params.set('MaxPrice', maxPrice.toString());
    if (sortBy) params = params.set('SortBy', sortBy);

    return this.http.get<PagedResult<Book>>(`${this.apiUrl}/subcategory/${subcategoryId}`, { params });
  }

  getBookById(id: number): Observable<Book> {
    return this.http.get<Book>(`${this.apiUrl}/${id}`);
  }

  searchBooks(searchTerm: string, page: number = 1, pageSize: number = 12, minPrice?: number, maxPrice?: number): Observable<PagedResult<Book>> {
    let params = new HttpParams()
      .set('SearchTerm', searchTerm)
      .set('Page', page.toString())
      .set('PageSize', pageSize.toString());

    if (minPrice != null) params = params.set('MinPrice', minPrice.toString());
    if (maxPrice != null) params = params.set('MaxPrice', maxPrice.toString());

    return this.http.get<PagedResult<Book>>(`${this.apiUrl}/search`, { params });
  }
}
