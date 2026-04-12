import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FamilyTreeData } from '../models/family-tree.model';

@Injectable({
  providedIn: 'root'
})
export class WikitreeService {
  private apiUrl = '/api/wikitree';

  constructor(private http: HttpClient) {}

  import(wikiTreeId: string, ancestorDepth: number = 5, descendantDepth: number = 2): Observable<FamilyTreeData> {
    return this.http.post<FamilyTreeData>(`${this.apiUrl}/import`, {
      wikiTreeId,
      ancestorDepth,
      descendantDepth
    });
  }

  expand(individualId: string, depth: number = 5): Observable<FamilyTreeData> {
    return this.http.post<FamilyTreeData>(`${this.apiUrl}/expand`, {
      individualId,
      depth
    });
  }
}
