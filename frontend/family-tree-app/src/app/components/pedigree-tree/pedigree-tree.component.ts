import {
  Component, Input, Output, EventEmitter,
  ElementRef, OnChanges, OnDestroy, SimpleChanges, AfterViewInit
} from '@angular/core';
import * as d3 from 'd3';
import { FamilyTreeData, Individual, AncestorNode } from '../../models/family-tree.model';
import { TreeDataService } from '../../services/tree-data.service';

interface TreeNode {
  person: Individual;
  children?: TreeNode[];
}

@Component({
  selector: 'app-pedigree-tree',
  standalone: true,
  template: '<div class="pedigree-container"></div>',
  styles: [`
    :host { display: block; width: 100%; height: 100%; }
    .pedigree-container { width: 100%; height: 100%; }
  `]
})
export class PedigreeTreeComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() data!: FamilyTreeData;
  @Input() rootPersonId!: string;
  @Output() personClicked = new EventEmitter<Individual>();

  private svg: any;
  private zoomGroup: any;
  private resizeObserver?: ResizeObserver;

  private readonly CARD_WIDTH = 180;
  private readonly CARD_HEIGHT = 72;
  private readonly HORIZONTAL_SPACING = 220;
  private readonly VERTICAL_SPACING = 90;

  constructor(
    private el: ElementRef,
    private treeDataService: TreeDataService
  ) {}

  ngAfterViewInit(): void {
    this.initSvg();
    this.render();

    this.resizeObserver = new ResizeObserver(() => this.render());
    this.resizeObserver.observe(this.el.nativeElement);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.svg && (changes['rootPersonId'] || changes['data'])) {
      this.render();
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  private initSvg(): void {
    const container = this.el.nativeElement.querySelector('.pedigree-container');
    this.svg = d3.select(container)
      .append('svg')
      .attr('width', '100%')
      .attr('height', '100%');

    this.zoomGroup = this.svg.append('g');

    const zoom = d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.1, 3])
      .on('zoom', (event: d3.D3ZoomEvent<SVGSVGElement, unknown>) => {
        this.zoomGroup.attr('transform', event.transform);
      });

    this.svg.call(zoom);
  }

  private render(): void {
    if (!this.data || !this.rootPersonId) return;

    this.zoomGroup.selectAll('*').remove();

    const ancestorTree = this.treeDataService.buildAncestorTree(this.rootPersonId, this.data);
    if (!ancestorTree) return;

    // Convert to d3 hierarchy format (person -> parents as children)
    const hierarchyData = this.convertToHierarchy(ancestorTree);
    const root = d3.hierarchy(hierarchyData);

    const treeLayout = d3.tree<TreeNode>()
      .nodeSize([this.VERTICAL_SPACING, this.HORIZONTAL_SPACING])
      .separation((a, b) => a.parent === b.parent ? 1 : 1.2);

    treeLayout(root);

    const container = this.el.nativeElement.querySelector('.pedigree-container');
    const width = container.clientWidth;
    const height = container.clientHeight;

    // Draw links
    this.zoomGroup.selectAll('.link')
      .data(root.links())
      .join('path')
      .attr('class', 'link')
      .attr('d', (d: d3.HierarchyLink<TreeNode>) => {
        const sx = (d.source as any).y + width / 4;
        const sy = (d.source as any).x + height / 2;
        const tx = (d.target as any).y + width / 4;
        const ty = (d.target as any).x + height / 2;
        const mx = (sx + tx) / 2;
        return `M${sx},${sy} C${mx},${sy} ${mx},${ty} ${tx},${ty}`;
      })
      .attr('fill', 'none')
      .attr('stroke', '#c8c4bc')
      .attr('stroke-width', 2);

    // Draw nodes
    const nodes = this.zoomGroup.selectAll('.node')
      .data(root.descendants())
      .join('g')
      .attr('class', 'node')
      .attr('transform', (d: d3.HierarchyPointNode<TreeNode>) =>
        `translate(${d.y + width / 4 - this.CARD_WIDTH / 2},${d.x + height / 2 - this.CARD_HEIGHT / 2})`)
      .style('cursor', 'pointer')
      .on('click', (_event: MouseEvent, d: d3.HierarchyPointNode<TreeNode>) => {
        this.personClicked.emit(d.data.person);
      });

    // Card background
    nodes.append('rect')
      .attr('width', this.CARD_WIDTH)
      .attr('height', this.CARD_HEIGHT)
      .attr('rx', 10)
      .attr('ry', 10)
      .attr('fill', '#ffffff')
      .attr('stroke', (d: d3.HierarchyPointNode<TreeNode>) =>
        d.data.person.id === this.rootPersonId ? '#2a7d3f' : '#e0ddd5')
      .attr('stroke-width', (d: d3.HierarchyPointNode<TreeNode>) =>
        d.data.person.id === this.rootPersonId ? 2.5 : 1.5)
      .attr('filter', 'drop-shadow(0 2px 4px rgba(0,0,0,0.08))');

    // Gender indicator bar
    nodes.append('rect')
      .attr('width', 4)
      .attr('height', this.CARD_HEIGHT - 16)
      .attr('x', 8)
      .attr('y', 8)
      .attr('rx', 2)
      .attr('fill', (d: d3.HierarchyPointNode<TreeNode>) =>
        d.data.person.sex === 'M' ? '#4a90d9' : d.data.person.sex === 'F' ? '#d94a8e' : '#95a5a6');

    // Name text
    nodes.append('text')
      .attr('x', 20)
      .attr('y', 28)
      .attr('font-size', '13px')
      .attr('font-weight', '600')
      .attr('fill', '#2c3e50')
      .attr('font-family', "'Inter', sans-serif")
      .text((d: d3.HierarchyPointNode<TreeNode>) =>
        this.treeDataService.getDisplayName(d.data.person))
      .each(function(this: SVGTextElement) {
        const textEl = d3.select(this);
        if ((this as SVGTextElement).getComputedTextLength() > 148) {
          let text = textEl.text();
          while ((this as SVGTextElement).getComputedTextLength() > 140 && text.length > 0) {
            text = text.slice(0, -1);
            textEl.text(text + '...');
          }
        }
      });

    // Lifespan text
    nodes.append('text')
      .attr('x', 20)
      .attr('y', 46)
      .attr('font-size', '11px')
      .attr('fill', '#7f8c8d')
      .attr('font-family', "'Inter', sans-serif")
      .text((d: d3.HierarchyPointNode<TreeNode>) =>
        this.treeDataService.getLifespan(d.data.person));

    // Generation label
    nodes.append('text')
      .attr('x', 20)
      .attr('y', 62)
      .attr('font-size', '9px')
      .attr('fill', '#bdc3c7')
      .attr('font-family', "'Inter', sans-serif")
      .text((d: d3.HierarchyPointNode<TreeNode>) => {
        const gen = d.depth;
        if (gen === 0) return 'Self';
        if (gen === 1) return 'Parent';
        if (gen === 2) return 'Grandparent';
        return `${gen}x Great-Grandparent`;
      });

    // Hover effects
    nodes.on('mouseenter', function(this: SVGGElement) {
      d3.select(this).select('rect:first-child')
        .transition().duration(200)
        .attr('filter', 'drop-shadow(0 4px 12px rgba(0,0,0,0.15))');
    }).on('mouseleave', function(this: SVGGElement) {
      d3.select(this).select('rect:first-child')
        .transition().duration(200)
        .attr('filter', 'drop-shadow(0 2px 4px rgba(0,0,0,0.08))');
    });
  }

  private convertToHierarchy(node: AncestorNode): TreeNode {
    const result: TreeNode = { person: node.person };
    const children: TreeNode[] = [];

    if (node.father) children.push(this.convertToHierarchy(node.father));
    if (node.mother) children.push(this.convertToHierarchy(node.mother));

    if (children.length > 0) result.children = children;
    return result;
  }
}
