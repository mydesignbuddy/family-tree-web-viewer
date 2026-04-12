import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FamilyDataStoreService } from '../../services/family-data-store.service';
import { GedcomService } from '../../services/gedcom.service';
import { WikitreeService } from '../../services/wikitree.service';
import { WikitreeAuthService, WikiTreeUser } from '../../services/wikitree-auth.service';
import { StoredTreeSummary } from '../../models/family-tree.model';

@Component({
  selector: 'app-tree-management',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatToolbarModule, MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatDividerModule, MatProgressSpinnerModule
  ],
  templateUrl: './tree-management.component.html',
  styleUrl: './tree-management.component.scss'
})
export class TreeManagementComponent implements OnInit {
  wikiTreeUser: WikiTreeUser | null = null;
  wikiTreeId = '';
  ancestorDepth = 5;
  descendantDepth = 2;
  isUploading = false;
  isImporting = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  trees: StoredTreeSummary[] = [];
  activeTreeId: string | null = null;

  constructor(
    public dataStore: FamilyDataStoreService,
    private gedcomService: GedcomService,
    private wikitreeService: WikitreeService,
    private wikitreeAuth: WikitreeAuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.wikitreeAuth.checkStatus().subscribe();
    this.wikitreeAuth.user$.subscribe(user => this.wikiTreeUser = user);
    this.refreshTreeList();
  }

  private refreshTreeList(): void {
    this.gedcomService.listTrees().subscribe({
      next: (res) => {
        this.trees = res.trees;
        this.activeTreeId = res.activeTreeId;
        this.dataStore.setTrees(res.trees, res.activeTreeId);
      }
    });
  }

  get activeTree(): StoredTreeSummary | null {
    return this.trees.find(t => t.id === this.activeTreeId) ?? null;
  }

  get treeName(): string {
    return this.activeTree?.name ?? 'No Tree Loaded';
  }

  get individualCount(): number {
    return this.activeTree?.individualCount ?? 0;
  }

  get familyCount(): number {
    return this.activeTree?.familyCount ?? 0;
  }

  get homePerson(): string {
    return this.activeTree?.rootPersonName ?? 'None';
  }

  goBack(): void {
    const rootId = this.dataStore.data?.rootIndividualId;
    if (rootId) {
      this.router.navigate(['/family-tree', 'pedigree-view', rootId]);
    } else {
      this.router.navigate(['/family-tree', 'pedigree-view']);
    }
  }

  // Tree switching
  onTreeSelected(treeId: string): void {
    if (treeId === this.activeTreeId) return;
    this.clearMessages();
    this.gedcomService.activateTree(treeId).subscribe({
      next: (data) => {
        this.activeTreeId = treeId;
        this.dataStore.setData(data);
        this.dataStore.setTrees(this.trees, treeId);
        this.successMessage = `Switched to "${this.trees.find(t => t.id === treeId)?.name}".`;
      },
      error: () => {
        this.errorMessage = 'Failed to switch tree.';
      }
    });
  }

  onDeleteTree(treeId: string): void {
    this.clearMessages();
    this.gedcomService.deleteTree(treeId).subscribe({
      next: (res) => {
        this.trees = res.trees;
        this.activeTreeId = res.activeTreeId;
        this.dataStore.setTrees(res.trees, res.activeTreeId);
        // If the active tree changed, load it
        if (res.activeTreeId) {
          this.gedcomService.activateTree(res.activeTreeId).subscribe({
            next: (data) => this.dataStore.setData(data)
          });
        } else {
          this.dataStore.setData(null!);
        }
        this.successMessage = 'Tree deleted.';
      }
    });
  }

  // GEDCOM Upload
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadFile(input.files[0]);
    }
  }

  private uploadFile(file: File): void {
    if (!file.name.toLowerCase().endsWith('.ged')) {
      this.errorMessage = 'Please select a valid GEDCOM file (.ged)';
      return;
    }
    this.clearMessages();
    this.isUploading = true;
    this.gedcomService.uploadFile(file).subscribe({
      next: (data) => {
        this.isUploading = false;
        this.dataStore.setData(data);
        this.refreshTreeList();
        this.successMessage = `Loaded ${Object.keys(data.individuals).length} individuals from "${file.name}".`;
      },
      error: (err) => {
        this.isUploading = false;
        this.errorMessage = err.error?.error || 'Failed to upload file.';
      }
    });
  }

  // WikiTree Auth
  loginToWikiTree(): void {
    this.wikitreeAuth.login();
  }

  logoutFromWikiTree(): void {
    this.wikitreeAuth.logout().subscribe();
  }

  // WikiTree Import (Public)
  importPublic(): void {
    if (!this.wikiTreeId.trim()) {
      this.errorMessage = 'Please enter a WikiTree ID.';
      return;
    }
    this.doWikiTreeImport();
  }

  // WikiTree Import (Authenticated)
  importAuthenticated(): void {
    if (!this.wikiTreeUser) return;
    this.wikiTreeId = this.wikiTreeUser.userName;
    this.doWikiTreeImport();
  }

  private doWikiTreeImport(): void {
    this.clearMessages();
    this.isImporting = true;
    this.wikitreeService.import(this.wikiTreeId.trim(), this.ancestorDepth, this.descendantDepth).subscribe({
      next: (data) => {
        this.isImporting = false;
        this.dataStore.setData(data);
        this.refreshTreeList();
        this.successMessage = `Loaded ${Object.keys(data.individuals).length} individuals from WikiTree.`;
      },
      error: (err) => {
        this.isImporting = false;
        this.errorMessage = err.error?.error || 'Failed to import from WikiTree.';
      }
    });
  }

  // Delete all trees
  deleteAllTrees(): void {
    this.gedcomService.clear().subscribe({
      next: () => {
        this.dataStore.clear();
        this.trees = [];
        this.activeTreeId = null;
        this.successMessage = 'All trees cleared.';
      },
      error: () => {
        this.dataStore.clear();
        this.trees = [];
        this.activeTreeId = null;
      }
    });
  }

  private clearMessages(): void {
    this.errorMessage = null;
    this.successMessage = null;
  }

  sourceIcon(source: string): string {
    if (source.startsWith('WikiTree')) return 'public';
    if (source === 'GEDCOM') return 'upload_file';
    return 'description';
  }
}
