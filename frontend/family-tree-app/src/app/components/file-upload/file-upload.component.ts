import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GedcomService } from '../../services/gedcom.service';
import { FamilyTreeData } from '../../models/family-tree.model';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-upload.component.html',
  styleUrl: './file-upload.component.scss'
})
export class FileUploadComponent {
  @Output() fileUploaded = new EventEmitter<FamilyTreeData>();

  isDragging = false;
  isUploading = false;
  errorMessage: string | null = null;

  constructor(private gedcomService: GedcomService) {}

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFile(files[0]);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  private handleFile(file: File): void {
    if (!file.name.toLowerCase().endsWith('.ged')) {
      this.errorMessage = 'Please select a valid GEDCOM file (.ged)';
      return;
    }

    this.errorMessage = null;
    this.isUploading = true;

    this.gedcomService.uploadFile(file).subscribe({
      next: (data) => {
        this.isUploading = false;
        this.fileUploaded.emit(data);
      },
      error: (err) => {
        this.isUploading = false;
        this.errorMessage = err.error?.error || 'Failed to upload file. Please try again.';
      }
    });
  }
}
