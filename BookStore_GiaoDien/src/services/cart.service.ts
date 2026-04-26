import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, forkJoin } from 'rxjs';
import { map, tap, switchMap, catchError } from 'rxjs/operators';
import { environment } from '../environments/environment';
import { AuthService } from './auth.service';
import { Cart, CartItem } from '../app/models/cart.model';
import { BookService } from './book.service';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private bookService = inject(BookService);
  private apiUrl = `${environment.apiUrl}/cart`;

  private cartSubject = new BehaviorSubject<Cart>({ id: 0, items: [], totalPrice: 0, totalItems: 0 });
  public cart$ = this.cartSubject.asObservable();

  constructor() {
    this.initCart();
  }

  private initCart() {
    if (this.authService.isLoggedIn()) {
      this.loadCartFromServer();
    } else {
      this.loadCartFromLocalStorage();
    }
  }

  loadCartFromServer() {
    this.http.get<any>(this.apiUrl).pipe(
      map(res => this.mapServerCart(res))
    ).subscribe(cart => {
      this.cartSubject.next(cart);
    });
  }

  loadCartFromLocalStorage() {
    const savedCart = localStorage.getItem('guest_cart');
    if (savedCart) {
      const items: CartItem[] = JSON.parse(savedCart);
      this.updateGuestCart(items);
    } else {
      this.cartSubject.next({ id: 0, items: [], totalPrice: 0, totalItems: 0 });
    }
  }

  addToCart(bookId: number, quantity: number = 1): Observable<any> {
    if (this.authService.isLoggedIn()) {
      return this.http.post(`${this.apiUrl}/add`, null, {
        params: { bookId: bookId.toString(), quantity: quantity.toString() }
      }).pipe(
        tap(() => this.loadCartFromServer())
      );
    } else {
      const currentItems = this.getGuestItems();
      const existingItem = currentItems.find(i => i.bookId === bookId);
      if (existingItem) {
        existingItem.quantity += quantity;
      } else {
        currentItems.push({ bookId, quantity } as CartItem);
      }
      this.updateGuestCart(currentItems);
      return of({ success: true });
    }
  }

  removeFromCart(bookId: number): Observable<any> {
    if (this.authService.isLoggedIn()) {
      return this.http.delete(`${this.apiUrl}/remove/${bookId}`).pipe(
        tap(() => this.loadCartFromServer())
      );
    } else {
      const currentItems = this.getGuestItems().filter(i => i.bookId !== bookId);
      this.updateGuestCart(currentItems);
      return of({ success: true });
    }
  }

  updateQuantity(bookId: number, quantity: number): Observable<any> {
    if (quantity <= 0) return this.removeFromCart(bookId);

    if (this.authService.isLoggedIn()) {
      return this.http.put(`${this.apiUrl}/update-quantity`, null, {
        params: { bookId: bookId.toString(), quantity: quantity.toString() }
      }).pipe(
        tap(() => this.loadCartFromServer())
      );
    } else {
      const currentItems = this.getGuestItems();
      const item = currentItems.find(i => i.bookId === bookId);
      if (item) {
        item.quantity = quantity;
        this.updateGuestCart(currentItems);
      }
      return of({ success: true });
    }
  }

  private getGuestItems(): CartItem[] {
    const saved = localStorage.getItem('guest_cart');
    return saved ? JSON.parse(saved) : [];
  }

  private updateGuestCart(items: CartItem[]) {
    localStorage.setItem('guest_cart', JSON.stringify(items.map(i => ({ bookId: i.bookId, quantity: i.quantity }))));
    
    if (items.length === 0) {
      this.cartSubject.next({ id: 0, items: [], totalPrice: 0, totalItems: 0 });
      return;
    }

    const requests = items.map(item => 
      this.bookService.getBookById(item.bookId).pipe(
        map((res: any) => {
          let rawBook = res.data || res;
          
          const book = {
            id: rawBook.id ?? rawBook.Id,
            bookTitle: rawBook.title ?? rawBook.Title,
            author: rawBook.author ?? rawBook.Author,
            imageUrl: rawBook.imageUrl ?? rawBook.ImageUrl,
            originalPrice: rawBook.price ?? rawBook.Price,
            price: (rawBook.isFlashSale && rawBook.discountPrice) ? rawBook.discountPrice : (rawBook.price ?? rawBook.Price)
          };
          
          return {
            ...item,
            bookTitle: book.bookTitle,
            imageUrl: book.imageUrl,
            author: book.author || 'Ẩn danh',
            originalPrice: book.originalPrice,
            price: book.price,
            subTotal: book.price * (item.quantity || 1)
          };
        }),
      )
    );
    forkJoin(requests).subscribe(fullItems => {
      const typedItems = fullItems as CartItem[];
      const totalPrice = typedItems.reduce((acc: number, item: CartItem) => acc + ((item.price || 0) * item.quantity), 0);
      const totalItems = typedItems.reduce((acc: number, item: CartItem) => acc + item.quantity, 0);
      this.cartSubject.next({ id: 0, items: typedItems, totalPrice, totalItems });
    });
  }

  private mapServerCart(res: any): Cart {
    let rawItems = res.items?.$values || res.Items?.$values || res.items || res.Items || [];
    
    const items: CartItem[] = rawItems.map((ri: any) => ({
      id: ri.id ?? ri.Id,
      bookId: ri.bookId ?? ri.BookId,
      quantity: ri.quantity ?? ri.Quantity,
      bookTitle: ri.bookTitle ?? ri.BookTitle ?? ri.title ?? ri.Title,
      imageUrl: ri.imageUrl ?? ri.ImageUrl,
      author: ri.author ?? ri.Author ?? '',
      originalPrice: ri.originalPrice ?? ri.OriginalPrice ?? ri.price ?? ri.Price,
      price: ri.price ?? ri.Price,
      subTotal: ri.subTotal ?? ri.SubTotal ?? ((ri.price ?? ri.Price) * (ri.quantity ?? ri.Quantity))
    }));

    const totalPrice = res.totalPrice ?? res.TotalPrice ?? items.reduce((acc: number, item: CartItem) => acc + (item.price * item.quantity), 0);
    const totalItems = items.reduce((acc: number, item: CartItem) => acc + item.quantity, 0);

    return { 
      id: res.id ?? res.Id ?? 0,
      items, 
      totalPrice,
      totalItems
    };
  }

  clearCart(): Observable<any> {
    if (this.authService.isLoggedIn()) {
      return this.http.delete(`${this.apiUrl}/clear`).pipe(
        tap(() => this.loadCartFromServer())
      );
    } else {
      localStorage.removeItem('guest_cart');
      this.cartSubject.next({ id: 0, items: [], totalPrice: 0, totalItems: 0 });
      return of({ success: true });
    }
  }

  clearLocalCart() {
    localStorage.removeItem('guest_cart');
    this.cartSubject.next({ id: 0, items: [], totalPrice: 0, totalItems: 0 });
  }
}
