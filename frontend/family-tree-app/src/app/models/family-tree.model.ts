export interface Individual {
  id: string;
  firstName: string;
  lastName: string;
  sex: string;
  birthDate?: string;
  birthPlace?: string;
  deathDate?: string;
  deathPlace?: string;
  familiesAsSpouse: string[];
  familyAsChild?: string;
}

export interface Family {
  id: string;
  husbandId?: string;
  wifeId?: string;
  childrenIds: string[];
  marriageDate?: string;
  marriagePlace?: string;
}

export interface FamilyTreeData {
  individuals: { [id: string]: Individual };
  families: { [id: string]: Family };
  rootIndividualId?: string;
}

export type ViewMode = 'pedigree' | 'descendant' | 'fan';

export interface AncestorNode {
  person: Individual;
  father?: AncestorNode;
  mother?: AncestorNode;
  generation: number;
}

export interface DescendantNode {
  person: Individual;
  spouse?: Individual;
  familyId?: string;
  children: DescendantNode[];
}
