import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FamilyTreeData } from '../models/family-tree.model';

@Injectable({
  providedIn: 'root'
})
export class GedcomService {
  private apiUrl = '/api/gedcom';

  constructor(private http: HttpClient) {}

  uploadFile(file: File): Observable<FamilyTreeData> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<FamilyTreeData>(`${this.apiUrl}/upload`, formData);
  }

  getCurrent(): Observable<FamilyTreeData> {
    return this.http.get<FamilyTreeData>(`${this.apiUrl}/current`);
  }

  clear(): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/current`);
  }
}
