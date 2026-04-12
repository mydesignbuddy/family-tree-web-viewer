import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { WikitreeAuthService } from '../../services/wikitree-auth.service';
import { WikitreeService } from '../../services/wikitree.service';
import { FamilyDataStoreService } from '../../services/family-data-store.service';
import { switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-wikitree-callback',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule, MatCardModule],
  template: `
    <div class="callback-container">
      <mat-card class="callback-card">
        @if (error) {
          <mat-card-content>
            <p class="error">{{ error }}</p>
            <p><a (click)="goHome()">Return to Family Tree Viewer</a></p>
          </mat-card-content>
        } @else {
          <mat-card-content class="loading">
            <mat-spinner diameter="40"></mat-spinner>
            <p>{{ statusMessage }}</p>
          </mat-card-content>
        }
      </mat-card>
    </div>
  `,
  styles: [`
    .callback-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #f5f5f0 0%, #e8e6df 100%);
    }
    .callback-card { max-width: 400px; width: 100%; text-align: center; padding: 2rem; }
    .loading { display: flex; flex-direction: column; align-items: center; gap: 1rem; }
    .error { color: #c62828; }
    a { cursor: pointer; color: #1976d2; text-decoration: underline; }
  `]
})
export class WikitreeCallbackComponent implements OnInit {
  error: string | null = null;
  statusMessage = 'Completing WikiTree authentication...';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: WikitreeAuthService,
    private wikitreeService: WikitreeService,
    private dataStore: FamilyDataStoreService
  ) {}

  ngOnInit(): void {
    const authCode = this.route.snapshot.queryParamMap.get('authcode');

    if (!authCode) {
      this.error = 'No authentication code received from WikiTree.';
      return;
    }

    this.authService.handleCallback(authCode).pipe(
      switchMap(user => {
        this.statusMessage = `Authenticated as ${user.userName}. Loading family tree...`;
        return this.wikitreeService.import(user.userName, 5, 2);
      })
    ).subscribe({
      next: (data) => {
        this.dataStore.setData(data);
        const rootId = data.rootIndividualId;
        this.router.navigate(rootId
          ? ['/family-tree', 'pedigree-view', rootId]
          : ['/family-tree', 'pedigree-view']);
      },
      error: (err) => {
        this.error = err.error?.error || 'Authentication failed. Please try again.';
      }
    });
  }

  goHome(): void {
    this.router.navigate(['/']);
  }
}
