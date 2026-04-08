import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileUploadComponent } from './components/file-upload/file-upload.component';
import { FamilyTreeViewerComponent } from './components/family-tree-viewer/family-tree-viewer.component';
import { PersonDetailPanelComponent } from './components/person-detail-panel/person-detail-panel.component';
import { FamilyTreeData, Individual } from './models/family-tree.model';
import { GedcomService } from './services/gedcom.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FileUploadComponent, FamilyTreeViewerComponent, PersonDetailPanelComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  familyData: FamilyTreeData | null = null;
  selectedPerson: Individual | null = null;

  constructor(private gedcomService: GedcomService) {}

  ngOnInit(): void {
    this.gedcomService.getCurrent().subscribe({
      next: (data) => this.onFileUploaded(data),
      error: () => {} // No stored data — show upload screen
    });
  }

  onFileUploaded(data: FamilyTreeData): void {
    this.familyData = data;
    if (data.rootIndividualId) {
      this.selectedPerson = data.individuals[data.rootIndividualId] || null;
    }
  }

  onPersonSelected(person: Individual): void {
    this.selectedPerson = person;
  }

  onNavigateToPerson(person: Individual): void {
    this.selectedPerson = person;
  }

  onGedcomCleared(): void {
    this.familyData = null;
    this.selectedPerson = null;
  }

  closeDetailPanel(): void {
    this.selectedPerson = null;
  }
}
