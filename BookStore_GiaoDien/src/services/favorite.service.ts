import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { Observable, BehaviorSubject, tap, map } from 'rxjs';
import { FavoriteDto } from '../app/models/favorite.model';

@Injectable({
  providedIn: 'root'
})
export class FavoriteService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/favorites`;

  // State management cho danh sách ID yêu thích
  private favoriteIdsSubject = new BehaviorSubject<number[]>([]);
  public favoriteIds$ = this.favoriteIdsSubject.asObservable();

  constructor() {
    // Khởi tạo danh sách nếu đã đăng nhập (Optional: có thể gọi ở component)
  }

  // Cập nhật danh sách từ Server
  refreshFavorites(): Observable<FavoriteDto[]> {
    return this.http.get<FavoriteDto[]>(`${this.apiUrl}`).pipe(
      tap(favs => {
        const data = this.ensureArray(favs);
        const ids = data.map(f => f.bookId || f.BookId);
        this.favoriteIdsSubject.next(ids);
      })
    );
  }

  // Lấy danh sách sản phẩm yêu thích (giữ lại để tương thích ngược)
  getFavorites(): Observable<FavoriteDto[]> {
    return this.refreshFavorites();
  }

  // Toggle trạng thái yêu thích
  toggleFavorite(bookId: number): Observable<{ isFavorited: boolean }> {
    return this.http.post<{ isFavorited: boolean }>(`${this.apiUrl}/toggle/${bookId}`, {}).pipe(
      tap(res => {
        const currentIds = this.favoriteIdsSubject.value;
        const isFav = res.isFavorited ?? (res as any).IsFavorited;
        
        let newIds: number[];
        if (isFav) {
          newIds = [...currentIds, bookId];
        } else {
          newIds = currentIds.filter(id => id !== bookId);
        }
        this.favoriteIdsSubject.next(newIds);
      })
    );
  }

  private ensureArray(data: any): any[] {
    if (!data) return [];
    if (Array.isArray(data)) return data;
    if (data.$values && Array.isArray(data.$values)) return data.$values;
    return [];
  }
}
