import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FamilyTreeData, Individual, ViewMode } from '../../models/family-tree.model';
import { TreeDataService } from '../../services/tree-data.service';
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

  viewMode: ViewMode = 'pedigree';
  selectedPersonId: string = '';

  constructor(private treeDataService: TreeDataService) {}

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

  onPersonClicked(person: Individual): void {
    this.selectedPersonId = person.id;
    this.personSelected.emit(person);
  }

  onNewFileClick(): void {
    window.location.reload();
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
