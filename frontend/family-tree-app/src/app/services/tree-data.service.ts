import { Injectable } from '@angular/core';
import {
  FamilyTreeData,
  Individual,
  Family,
  AncestorNode,
  DescendantNode
} from '../models/family-tree.model';

@Injectable({
  providedIn: 'root'
})
export class TreeDataService {

  buildAncestorTree(
    personId: string,
    data: FamilyTreeData,
    generation: number = 0,
    maxGenerations: number = 6
  ): AncestorNode | null {
    const person = data.individuals[personId];
    if (!person) return null;

    const node: AncestorNode = { person, generation };

    if (generation >= maxGenerations) return node;

    if (person.familyAsChild) {
      const family = data.families[person.familyAsChild];
      if (family) {
        if (family.husbandId) {
          node.father = this.buildAncestorTree(family.husbandId, data, generation + 1, maxGenerations) ?? undefined;
        }
        if (family.wifeId) {
          node.mother = this.buildAncestorTree(family.wifeId, data, generation + 1, maxGenerations) ?? undefined;
        }
      }
    }

    return node;
  }

  buildDescendantTree(
    personId: string,
    data: FamilyTreeData,
    maxGenerations: number = 6,
    generation: number = 0
  ): DescendantNode | null {
    const person = data.individuals[personId];
    if (!person) return null;

    const node: DescendantNode = { person, children: [] };

    if (generation >= maxGenerations) return node;

    if (person.familiesAsSpouse && person.familiesAsSpouse.length > 0) {
      const familyId = person.familiesAsSpouse[0];
      const family = data.families[familyId];
      if (family) {
        node.familyId = familyId;
        const spouseId = family.husbandId === personId ? family.wifeId : family.husbandId;
        if (spouseId) {
          node.spouse = data.individuals[spouseId];
        }

        for (const childId of family.childrenIds) {
          const childNode = this.buildDescendantTree(childId, data, maxGenerations, generation + 1);
          if (childNode) {
            node.children.push(childNode);
          }
        }
      }
    }

    return node;
  }

  flattenAncestors(
    node: AncestorNode | null,
    position: number = 0,
    generation: number = 0
  ): Array<{ person: Individual; generation: number; position: number }> {
    if (!node) return [];

    const result = [{ person: node.person, generation, position }];

    if (node.father) {
      result.push(...this.flattenAncestors(node.father, position * 2, generation + 1));
    }
    if (node.mother) {
      result.push(...this.flattenAncestors(node.mother, position * 2 + 1, generation + 1));
    }

    return result;
  }

  getDisplayName(person: Individual): string {
    return `${person.firstName} ${person.lastName}`.trim() || 'Unknown';
  }

  getLifespan(person: Individual): string {
    const birth = this.extractYear(person.birthDate);
    const death = this.extractYear(person.deathDate);
    if (birth && death) return `${birth} - ${death}`;
    if (birth) return `b. ${birth}`;
    if (death) return `d. ${death}`;
    return '';
  }

  private extractYear(dateStr?: string): string | null {
    if (!dateStr) return null;
    const match = dateStr.match(/\d{4}/);
    return match ? match[0] : null;
  }
}
