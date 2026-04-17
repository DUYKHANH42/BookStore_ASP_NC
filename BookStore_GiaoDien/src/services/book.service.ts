import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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
    return this.http.get<Book[]>(this.apiUrl);
  }

  getBooksByCategory(categoryId: number, page: number = 1, pageSize: number = 12): Observable<PagedResult<Book>> {
    return this.http.get<PagedResult<Book>>(`${this.apiUrl}/category/${categoryId}?page=${page}&pageSize=${pageSize}`);
  }

  getBooksBySubcategory(subcategoryId: number, page: number = 1, pageSize: number = 12): Observable<PagedResult<Book>> {
    return this.http.get<PagedResult<Book>>(`${this.apiUrl}/subcategory/${subcategoryId}?page=${page}&pageSize=${pageSize}`);
  }
}
