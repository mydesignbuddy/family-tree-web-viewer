import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Individual, FamilyTreeData } from '../../models/family-tree.model';
import { TreeDataService } from '../../services/tree-data.service';

@Component({
  selector: 'app-person-detail-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './person-detail-panel.component.html',
  styleUrl: './person-detail-panel.component.scss'
})
export class PersonDetailPanelComponent {
  @Input() person!: Individual;
  @Input() data!: FamilyTreeData;
  @Output() closed = new EventEmitter<void>();
  @Output() navigateTo = new EventEmitter<Individual>();

  constructor(public treeDataService: TreeDataService) {}

  get parents(): Individual[] {
    if (!this.person.familyAsChild) return [];
    const family = this.data.families[this.person.familyAsChild];
    if (!family) return [];
    const parents: Individual[] = [];
    if (family.husbandId && this.data.individuals[family.husbandId])
      parents.push(this.data.individuals[family.husbandId]);
    if (family.wifeId && this.data.individuals[family.wifeId])
      parents.push(this.data.individuals[family.wifeId]);
    return parents;
  }

  get spouses(): { spouse: Individual; marriageDate?: string; marriagePlace?: string }[] {
    const result: { spouse: Individual; marriageDate?: string; marriagePlace?: string }[] = [];
    for (const famId of this.person.familiesAsSpouse) {
      const family = this.data.families[famId];
      if (!family) continue;
      const spouseId = family.husbandId === this.person.id ? family.wifeId : family.husbandId;
      if (spouseId && this.data.individuals[spouseId]) {
        result.push({
          spouse: this.data.individuals[spouseId],
          marriageDate: family.marriageDate,
          marriagePlace: family.marriagePlace
        });
      }
    }
    return result;
  }

  get children(): Individual[] {
    const result: Individual[] = [];
    for (const famId of this.person.familiesAsSpouse) {
      const family = this.data.families[famId];
      if (!family) continue;
      for (const childId of family.childrenIds) {
        if (this.data.individuals[childId]) {
          result.push(this.data.individuals[childId]);
        }
      }
    }
    return result;
  }

  get siblings(): Individual[] {
    if (!this.person.familyAsChild) return [];
    const family = this.data.families[this.person.familyAsChild];
    if (!family) return [];
    return family.childrenIds
      .filter(id => id !== this.person.id)
      .map(id => this.data.individuals[id])
      .filter(p => !!p);
  }

  onNavigate(person: Individual): void {
    this.navigateTo.emit(person);
  }
}
