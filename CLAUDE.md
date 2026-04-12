# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Family tree web viewer: Angular 18 frontend with D3.js visualizations, backed by a .NET API following FTDNA's DDD architecture. Users upload GEDCOM files or import from WikiTree to view family trees in pedigree, descendant, and fan chart modes.

## Commands

### Frontend (from `frontend/family-tree-app/`)
- **Dev server:** `ng serve` (or `npm start`)
- **Build:** `ng build`
- **Run tests:** `ng test`
- **Run single test:** `ng test --include='**/component-name*.spec.ts'`

### Backend (from `backend/`)
- **Build:** `dotnet build FTDNA.Services.FamilyTreeV3.sln`
- **Run API:** `dotnet run --project FTDNA.Services.FamilyTreeV3.Api`
- **Run tests:** `dotnet test`

## Architecture

### Frontend
- **Angular 18** with standalone components, Angular Material, D3.js for tree rendering
- **State:** `FamilyDataStoreService` holds tree data as RxJS BehaviorSubjects — components subscribe reactively
- **Data sources:** `GedcomService` (REST API for GEDCOM upload/storage), `WikitreeService` (WikiTree API integration)
- **Visualization components:** `pedigree-tree`, `descendant-tree`, `fan-chart` — all D3-based, rendered inside the `family-tree-viewer` shell component which switches views via route data
- **Routes:** `/family-tree/tree-management` (home/upload), `/family-tree/{pedigree,family,fan}-view/:personId`

### Backend (.NET DDD — follows FTDNA_ARCH_STANDARDS.md)
Five-project layout under `backend/`:
- **Api** — Controllers, Program.cs composition root
- **Common** — Shared config, interfaces, DTOs
- **Domain** — Business logic, no infrastructure dependencies
- **Data.Internal** — FTDNA data access
- **Data.External** — Third-party integrations (WikiTree)

Dependency flow: Api → Domain → Common ← Data.* (outer depends on inner, never reverse).

## WikiTree Integration

The app integrates with WikiTree's API for importing public profiles. API calls require a `User-Agent` header to avoid WAF blocks. See `wikitree.service.ts` and `wikitree-auth.service.ts`.
