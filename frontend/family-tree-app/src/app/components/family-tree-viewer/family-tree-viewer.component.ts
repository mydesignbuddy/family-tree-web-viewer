import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FamilyTreeData, Individual, ViewMode } from '../../models/family-tree.model';
import { TreeDataService } from '../../services/tree-data.service';
import { GedcomService } from '../../services/gedcom.service';
import { PedigreeTreeComponent } from '../pedigree-tree/pedigree-tree.component';
import { DescendantTreeComponent } from '../descendant-tree/descendant-tree.component';
import { FanChartComponent } from '../fan-chart/fan-chart.component';

@Component({
  selector: 'app-family-tree-viewer',
  standalone: true,
  imports: [CommonModule, PedigreeTreeComponent, DescendantTreeComponent, FanChartComponent],
  templateUrl: './family-tree-viewer.component.html',
  styleUrl: './family-tree-viewer.component.scss'
})
export class FamilyTreeViewerComponent {
  @Input() data!: FamilyTreeData;
  @Output() personSelected = new EventEmitter<Individual>();
  @Output() cleared = new EventEmitter<void>();

  viewMode: ViewMode = 'pedigree';
  selectedPersonId: string = '';
  navigationHistory: string[] = [];
  backPathPersons: Individual[] = [];

  constructor(private treeDataService: TreeDataService, private gedcomService: GedcomService) {}

  ngOnInit(): void {
    if (this.data.rootIndividualId) {
      this.selectedPersonId = this.data.rootIndividualId;
    }
  }

  get selectedPerson(): Individual | null {
    return this.data.individuals[this.selectedPersonId] || null;
  }

  get selectedPersonName(): string {
    const person = this.selectedPerson;
    if (!person) return '';
    return this.treeDataService.getDisplayName(person);
  }

  setViewMode(mode: ViewMode): void {
    this.viewMode = mode;
  }

  private updateBackPath(): void {
    this.backPathPersons = this.navigationHistory
      .slice(-1)
      .reverse()
      .map(id => this.data.individuals[id])
      .filter((p): p is Individual => !!p);
  }

  onPersonClicked(person: Individual): void {
    this.navigationHistory = [...this.navigationHistory, this.selectedPersonId];
    this.selectedPersonId = person.id;
    this.updateBackPath();
    this.personSelected.emit(person);
  }

  onNavigateBack(personId: string): void {
    this.navigationHistory = this.navigationHistory.slice(0, -1);
    this.selectedPersonId = personId;
    this.updateBackPath();
    const person = this.data.individuals[personId];
    if (person) {
      this.personSelected.emit(person);
    }
  }

  onNewFileClick(): void {
    window.location.reload();
  }

  onClearClick(): void {
    this.gedcomService.clear().subscribe({
      next: () => this.cleared.emit(),
      error: () => this.cleared.emit() // clear frontend state regardless
    });
  }

  get individualsList(): Individual[] {
    return Object.values(this.data.individuals);
  }

  selectPerson(id: string): void {
    this.selectedPersonId = id;
    const person = this.data.individuals[id];
    if (person) {
      this.personSelected.emit(person);
    }
  }
}
