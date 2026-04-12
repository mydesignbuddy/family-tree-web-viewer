import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';
import { FamilyTreeData, Individual, StoredTreeSummary } from '../models/family-tree.model';

@Injectable({
  providedIn: 'root'
})
export class FamilyDataStoreService {
  private dataSubject = new BehaviorSubject<FamilyTreeData | null>(null);
  data$ = this.dataSubject.asObservable();

  private treesSubject = new BehaviorSubject<StoredTreeSummary[]>([]);
  trees$ = this.treesSubject.asObservable();

  private activeTreeIdSubject = new BehaviorSubject<string | null>(null);
  activeTreeId$ = this.activeTreeIdSubject.asObservable();

  private viewProfileSubject = new Subject<Individual>();
  viewProfile$ = this.viewProfileSubject.asObservable();

  viewProfile(person: Individual): void {
    this.viewProfileSubject.next(person);
  }

  get data(): FamilyTreeData | null {
    return this.dataSubject.value;
  }

  get trees(): StoredTreeSummary[] {
    return this.treesSubject.value;
  }

  get activeTreeId(): string | null {
    return this.activeTreeIdSubject.value;
  }

  setData(data: FamilyTreeData): void {
    this.dataSubject.next(data);
  }

  setTrees(trees: StoredTreeSummary[], activeTreeId: string | null): void {
    this.treesSubject.next(trees);
    this.activeTreeIdSubject.next(activeTreeId);
  }

  showDetails = false;

  clear(): void {
    this.dataSubject.next(null);
    this.treesSubject.next([]);
    this.activeTreeIdSubject.next(null);
  }
}
