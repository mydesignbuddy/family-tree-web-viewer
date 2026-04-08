import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileUploadComponent } from './components/file-upload/file-upload.component';
import { FamilyTreeViewerComponent } from './components/family-tree-viewer/family-tree-viewer.component';
import { PersonDetailPanelComponent } from './components/person-detail-panel/person-detail-panel.component';
import { FamilyTreeData, Individual } from './models/family-tree.model';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FileUploadComponent, FamilyTreeViewerComponent, PersonDetailPanelComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  familyData: FamilyTreeData | null = null;
  selectedPerson: Individual | null = null;

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

  closeDetailPanel(): void {
    this.selectedPerson = null;
  }
}
