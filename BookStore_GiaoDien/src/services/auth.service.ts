import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { environment } from '../environments/environment';
import { AuthResponseDto, LoginDto, RegisterDto } from '../app/models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/auth`;
  
  private currentUserSubject = new BehaviorSubject<AuthResponseDto | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor() {
    const token = localStorage.getItem('token');
    const fullName = localStorage.getItem('fullName');
    const email = localStorage.getItem('email');

    if (token) {
      this.currentUserSubject.next({
        isSuccess: true,
        message: 'Loaded from storage',
        token,
        fullName: localStorage.getItem('fullName') || undefined,
        email: localStorage.getItem('email') || undefined,
        address: localStorage.getItem('address') || undefined,
        phoneNumber: localStorage.getItem('phoneNumber') || undefined,
        avtUrl: localStorage.getItem('avtUrl') || undefined,
        isActive: localStorage.getItem('isActive') === 'true'
      });
    }
  }

  // Đăng nhập
  login(data: LoginDto): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${this.apiUrl}/login`, data).pipe(
      map(res => {
        if (res.isSuccess && res.token) {
          localStorage.setItem('token', res.token);
          if (res.fullName) localStorage.setItem('fullName', res.fullName);
          if (res.email) localStorage.setItem('email', res.email);
          if (res.address) localStorage.setItem('address', res.address);
          if (res.phoneNumber) localStorage.setItem('phoneNumber', res.phoneNumber);
          if (res.avtUrl) localStorage.setItem('avtUrl', res.avtUrl);
          if (res.isActive !== undefined) {
            localStorage.setItem('isActive', String(res.isActive));
          }
          
          this.currentUserSubject.next(res);
        }
        return res;
      })
    );
  }

  // Đăng ký
  register(data: RegisterDto): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${this.apiUrl}/register`, data);
  }

  // Kiểm tra email trùng (Async Validator)
  checkEmailExists(email: string): Observable<boolean> {
    if (!email) return of(false);
    return this.http.get<boolean>(`${this.apiUrl}/check-email?email=${email}`).pipe(
      catchError(() => of(false))
    );
  }

  // Đăng xuất
  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('fullName');
    localStorage.removeItem('email');
    localStorage.removeItem('address');
    localStorage.removeItem('phoneNumber');
    localStorage.removeItem('avtUrl');
    localStorage.removeItem('isActive');
    this.currentUserSubject.next(null);
  }

  // Lấy token hiện tại
  getToken(): string | null {
    return localStorage.getItem('token');
  }

  // Kiểm tra trạng thái đăng nhập
  isLoggedIn(): boolean {
    return !!this.getToken();
  }
}

