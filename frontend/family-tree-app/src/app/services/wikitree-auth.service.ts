import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';

export interface WikiTreeUser {
  userId: string;
  userName: string;
}

export interface WikiTreeAuthStatus {
  authenticated: boolean;
  userId?: string;
  userName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class WikitreeAuthService {
  private apiUrl = '/api/wikitree/auth';
  private userSubject = new BehaviorSubject<WikiTreeUser | null>(null);
  user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient) {}

  get user(): WikiTreeUser | null {
    return this.userSubject.value;
  }

  login(): void {
    const callbackUrl = `${window.location.origin}/wikitree/callback`;
    this.http.get<{ loginUrl: string }>(`${this.apiUrl}/login`, {
      params: { returnUrl: callbackUrl }
    }).subscribe({
      next: (res) => {
        window.location.href = res.loginUrl;
      }
    });
  }

  handleCallback(authCode: string): Observable<WikiTreeUser> {
    return this.http.post<WikiTreeUser>(`${this.apiUrl}/callback`, { authCode }).pipe(
      tap(user => this.userSubject.next(user))
    );
  }

  checkStatus(): Observable<WikiTreeAuthStatus> {
    return this.http.get<WikiTreeAuthStatus>(`${this.apiUrl}/status`).pipe(
      tap(status => {
        if (status.authenticated) {
          this.userSubject.next({ userId: status.userId!, userName: status.userName! });
        } else {
          this.userSubject.next(null);
        }
      })
    );
  }

  logout(): Observable<{ success: boolean }> {
    return this.http.post<{ success: boolean }>(`${this.apiUrl}/logout`, {}).pipe(
      tap(() => this.userSubject.next(null))
    );
  }
}
